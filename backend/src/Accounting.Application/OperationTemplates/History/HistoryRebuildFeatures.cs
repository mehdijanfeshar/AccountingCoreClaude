using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.OperationTemplates;
using FluentValidation;
using MediatR;

namespace Accounting.Application.OperationTemplates.History;

// ═══════════════════ بازسازی جدول سوابق — فقط مدیر ستاد مرکزی ═══════════════════
// منبع: سندهای تأیید دائم/بررسی‌شده، حذف‌نشده، بدون افتتاحیه/اختتامیه/خودکار/اصلاحی، ۲ تا ۸ ردیف با معین فعال.
// امضا = مجموعهٔ یکتای (معین، سمت). تجمیع در اوراکل (SQL خام)؛ در حافظه فقط نمونهٔ محدود شرح‌ها برای کلمهٔ کلیدی.

/// <param name="RestoreDismissed">نادیده گرفته می‌شود (سازگاری با فرانت قدیمی)؛ هر بازسازی حذف‌شده‌ها را هم دوباره کشف می‌کند.</param>
public sealed record RebuildHistoryPatternsCommand(int FromYear, int ToYear, bool RestoreDismissed = false) : IRequest<HistoryRebuildResult>;

public sealed class RebuildHistoryPatternsValidator : AbstractValidator<RebuildHistoryPatternsCommand>
{
    public const int MaxSpanYears = 10;

    public RebuildHistoryPatternsValidator()
    {
        RuleFor(x => x.FromYear).InclusiveBetween(1300, 1600).WithMessage("سال شروع نامعتبر است.");
        RuleFor(x => x.ToYear).InclusiveBetween(1300, 1600).WithMessage("سال پایان نامعتبر است.");
        RuleFor(x => x).Must(x => x.FromYear <= x.ToYear).WithMessage("سال شروع باید از سال پایان بیشتر نباشد.")
            .Must(x => x.ToYear - x.FromYear < MaxSpanYears).WithMessage($"بازه حداکثر {MaxSpanYears} سال است.");
    }
}

public sealed class RebuildHistoryPatternsHandler : IRequestHandler<RebuildHistoryPatternsCommand, HistoryRebuildResult>
{
    private const int GlobalSample = 100;
    private const int UnitSample = 5;
    private const int GlobalKeywords = 25;
    private const int UnitKeywords = 10;
    private const int AccountChunk = 500;

    private readonly IHistorySourceReader _source;
    private readonly IHistoryPatternStore _store;
    private readonly ISubsidiaryAccountReader _accounts;
    private readonly IUnitAccessReadRepository _units;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IHeadquartersAccessService? _hq;
    private readonly IHistoryEmbeddingSignal? _embeddings;

    public RebuildHistoryPatternsHandler(IHistorySourceReader source, IHistoryPatternStore store, ISubsidiaryAccountReader accounts,
        IUnitAccessReadRepository units, IUnitOfWork unitOfWork, ICurrentUser currentUser, IHeadquartersAccessService? hq = null,
        IHistoryEmbeddingSignal? embeddings = null)
    { _source = source; _store = store; _accounts = accounts; _units = units; _unitOfWork = unitOfWork; _currentUser = currentUser; _hq = hq; _embeddings = embeddings; }

    public async Task<HistoryRebuildResult> Handle(RebuildHistoryPatternsCommand c, CancellationToken ct)
    {
        await TemplateMapping.EnsureAbilityAsync(_currentUser, _hq, AbilityCatalog.TemplatesDefine, "سوابق حسابیار", ct);
        var sw = Stopwatch.StartNew();

        // ۱. تجمیع در اوراکل: ردیف سطح الگو و ردیف (الگو، واحد)
        var agg = await _source.ReadAggregatesAsync(c.FromYear, c.ToYear, HistoryDefaults.MinPatternVouchers, ct);
        var patternRows = agg.Where(a => a.IsPattern).ToDictionary(a => a.Sig, StringComparer.Ordinal);
        var unitRows = agg.Where(a => !a.IsPattern && a.Vahed is not null && patternRows.ContainsKey(a.Sig)).ToList();

        // ۲. سهم میانهٔ هر ردیف
        var shares = (await _source.ReadSharesAsync(c.FromYear, c.ToYear, HistoryDefaults.MinPatternVouchers, ct))
            .Where(s => patternRows.ContainsKey(s.Sig))
            .ToLookup(s => s.Sig, StringComparer.Ordinal);

        // ۳. سطح‌های تفصیلی هر معین (تعریف فعلی کدینگ)
        var moinIds = patternRows.Keys.SelectMany(ParseSignature).Select(p => p.Moin).Distinct().ToList();
        var accounts = new Dictionary<Guid, SubsidiaryAccountInfo>();
        foreach (var chunk in moinIds.Chunk(AccountChunk))
            foreach (var kv in await _accounts.GetByIdsAsync(chunk, ct))
                accounts[kv.Key] = kv.Value;

        // ۴. نمونهٔ شرح‌ها ⇒ کلمهٔ کلیدی (جریانی؛ فقط شمارنده‌ها نگه داشته می‌شود)
        var globalTokens = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        var unitTokens = new Dictionary<(string Sig, string Vahed), Dictionary<string, int>>();
        var sampleDescs = new Dictionary<string, List<(int Rank, string Desc)>>(StringComparer.Ordinal);
        await foreach (var s in _source.ReadSamplesAsync(c.FromYear, c.ToYear, HistoryDefaults.MinPatternVouchers, GlobalSample, UnitSample, ct))
        {
            if (!patternRows.ContainsKey(s.Sig)) continue;
            var tokens = HistoryText.DistinctTokens(s.HeadDesc + " " + s.LineDescs);
            if (s.GlobalRank <= GlobalSample)
            {
                Bump(globalTokens.TryGetValue(s.Sig, out var g) ? g : globalTokens[s.Sig] = new(StringComparer.Ordinal), tokens);
                if (!string.IsNullOrWhiteSpace(s.HeadDesc))
                {
                    if (!sampleDescs.TryGetValue(s.Sig, out var list)) sampleDescs[s.Sig] = list = [];
                    list.Add((s.GlobalRank, s.HeadDesc.Trim()));
                }
            }
            if (s.UnitRank <= UnitSample)
            {
                var key = (s.Sig, s.Vahed);
                Bump(unitTokens.TryGetValue(key, out var u) ? u : unitTokens[key] = new(StringComparer.Ordinal), tokens);
            }
        }

        // ۵. نوع واحدها
        var typeOfUnit = (await _units.GetAllUnitsAsync(ct)).Where(u => u.TypeCode is not null)
            .ToDictionary(u => u.VahedCode, u => u.TypeCode!, StringComparer.Ordinal);

        // ۶. ساخت موجودیت‌ها. تصمیم صاحب پروژه (۲۰۲۶-۱۰-۱۸): هر بازسازی اسناد گذشته را از نو بررسی می‌کند و
        // هرچه یافت پیشنهاد می‌دهد؛ «حذف» فقط فهرست فعلی را پاک می‌کند و در بازسازی بعدی اثری ندارد.
        await _store.ClearDismissedAsync(ct);
        var now = DateTime.UtcNow;
        var patterns = new List<HistoryPattern>(patternRows.Count);
        var units = new List<HistoryPatternUnit>(unitRows.Count);
        var idOfSig = new Dictionary<string, Guid>(StringComparer.Ordinal);
        var unitsBySig = unitRows.ToLookup(u => u.Sig, StringComparer.Ordinal);

        foreach (var (sig, row) in patternRows)
        {
            // ترتیب امضا را اوراکل (NLS_SORT) تعیین کرده؛ برای مقایسهٔ پایدار با الگوها در C# دوباره مرتب می‌شود.
            var canon = HistoryJson.SignatureOf(ParseSignature(sig));
            var id = StableId(canon);
            idOfSig[sig] = id;
            var lines = BuildLines(sig, shares[sig].ToList(), accounts);
            var types = unitsBySig[sig].Select(u => typeOfUnit.GetValueOrDefault(u.Vahed!)).Where(t => t is not null)
                .Distinct().OrderBy(t => t, StringComparer.Ordinal).ToList();

            patterns.Add(new HistoryPattern
            {
                Id = id,
                Signature = canon,
                LinesJson = HistoryJson.Serialize(lines),
                VoucherCount = row.Count,
                UnitCount = row.Units,
                Years = row.MinYear == row.MaxYear ? row.MinYear.ToString(CultureInfo.InvariantCulture)
                    : $"{row.MinYear}-{row.MaxYear}",
                FromYear = row.MinYear,
                ToYear = row.MaxYear,
                MedianAmount = row.Median,
                LastDate = row.LastDate,
                KeywordsJson = globalTokens.TryGetValue(sig, out var gt)
                    ? HistoryJson.KeywordsFit(HistoryText.Top(gt, GlobalKeywords), 1900) : null,
                SampleDescsJson = sampleDescs.TryGetValue(sig, out var sd) ? SamplesJson(sd) : null,
                VahedTypes = types.Count == 0 ? null : string.Join(',', types),
                RebuiltAtUtc = now,
            });
        }

        foreach (var u in unitRows.Where(u => idOfSig.ContainsKey(u.Sig)))
        {
            units.Add(new HistoryPatternUnit
            {
                PatternId = idOfSig[u.Sig],
                VahedCode = u.Vahed!,
                VoucherCount = u.Count,
                KeywordsJson = unitTokens.TryGetValue((u.Sig, u.Vahed!), out var ut)
                    ? HistoryJson.KeywordsFit(HistoryText.Top(ut, UnitKeywords), 900) : null,
            });
        }

        // ۷. جایگزینی کامل در یک تراکنش
        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await _store.ReplaceAllAsync(patterns, units, ct);
            await _unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }

        _embeddings?.Request(); // بردار معنایی الگوهای تازه در پس‌زمینه (DDL 089)
        sw.Stop();
        return new HistoryRebuildResult(patterns.Count, patterns.Sum(p => p.VoucherCount ?? 0), sw.ElapsedMilliseconds);
    }

    private static void Bump(Dictionary<string, int> counts, IReadOnlySet<string> tokens)
    {
        foreach (var t in tokens) counts[t] = counts.GetValueOrDefault(t) + 1;
    }

    /// <summary>شناسهٔ پایدار از امضا تا شناسهٔ الگو بین بازسازی‌ها عوض نشود.</summary>
    public static Guid StableId(string signature) => new(MD5.HashData(Encoding.UTF8.GetBytes(signature)));

    public static IEnumerable<(Guid Moin, int Side)> ParseSignature(string sig)
    {
        foreach (var part in sig.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = part.LastIndexOf(':');
            if (i > 0 && Guid.TryParse(part[..i].Trim(), out var moin) && int.TryParse(part[(i + 1)..], out var side))
                yield return (moin, side);
        }
    }

    private static List<HistoryLine> BuildLines(string sig, IReadOnlyList<HistoryShareRow> shares, IReadOnlyDictionary<Guid, SubsidiaryAccountInfo> accounts) =>
        ParseSignature(sig)
            .Select(p =>
            {
                var share = shares.FirstOrDefault(s => s.Moin == p.Moin && s.Side == p.Side)?.Share ?? 0m;
                var levels = accounts.TryGetValue(p.Moin, out var acc)
                    ? acc.DetailLevels.OrderBy(l => l.Level)
                        .Select(l => new HistoryLevel(l.Level, l.LevelId, l.DetailGroupIds.OrderBy(g => g).ToList())).ToList()
                    : [];
                return new HistoryLine(p.Moin, p.Side, Math.Round(share, 2, MidpointRounding.AwayFromZero), levels);
            })
            .OrderBy(l => l.Side).ThenByDescending(l => l.Share).ToList();

    private static string? SamplesJson(List<(int Rank, string Desc)> list)
    {
        var picked = list.OrderBy(x => x.Rank).Select(x => x.Desc.Length > 150 ? x.Desc[..150] : x.Desc)
            .Distinct(StringComparer.Ordinal).Take(5).ToList();
        while (picked.Count > 0)
        {
            var s = HistoryJson.Serialize(picked);
            if (s.Length <= 1900) return s;
            picked.RemoveAt(picked.Count - 1);
        }
        return null;
    }
}
