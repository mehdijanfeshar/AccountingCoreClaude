using System.Globalization;
using System.Text.Json.Serialization;
using Accounting.Application.Commands.Vouchers.Common;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Domain.OperationTemplates;
using FluentValidation;
using MediatR;

namespace Accounting.Application.OperationTemplates.History;

// ═══════════════════ تطبیق جملهٔ کاربر با سوابق (کاربر بدون دانش حسابداری) ═══════════════════
// وقتی جمله به هیچ الگوی ستاد نمی‌خورد. فقط حساب‌ها و ساختار برمی‌گردد؛ هرگز تفصیلی (نام طرف حساب) واحدهای دیگر.

public sealed record MatchHistoryPatternsQuery(string Sentence) : IRequest<IReadOnlyList<HistoryMatchDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;

    /// <summary>سال مالی جاری کاربر — از query (<c>?year=</c>)، هرگز از بدنه.</summary>
    [JsonIgnore]
    public string? Year { get; set; }
}

public sealed class MatchHistoryPatternsValidator : AbstractValidator<MatchHistoryPatternsQuery>
{
    public MatchHistoryPatternsValidator() =>
        RuleFor(x => x.Sentence).NotEmpty().WithMessage("جمله را بنویسید.")
            .MaximumLength(1000).WithMessage("جمله حداکثر ۱۰۰۰ نویسه است.");
}

/// <param name="Questions">پارامترهای الگوی موقت — همان DTO پارامتر الگوها (مبلغ، طرف حساب‌ها، تاریخ و توضیح اختیاری).</param>
public sealed record HistoryMatchDto(
    Guid PatternId, string PlainSummary, IReadOnlyList<string> SampleDescriptions, int VoucherCount, int InMyUnitCount,
    IReadOnlyList<TemplateParameterDto> Questions);

public sealed class MatchHistoryPatternsHandler : IRequestHandler<MatchHistoryPatternsQuery, IReadOnlyList<HistoryMatchDto>>
{
    private const int Top = 3;
    private const int MaxTokens = 12;
    private const int CandidateTake = 150;
    private const int EvaluateAtMost = 25;

    private readonly IHistoryPatternStore _store;
    private readonly HistoryTemplateFactory _factory;
    private readonly IAccountEntryPolicy _policy;

    public MatchHistoryPatternsHandler(IHistoryPatternStore store, HistoryTemplateFactory factory, IAccountEntryPolicy policy)
    { _store = store; _factory = factory; _policy = policy; }

    public async Task<IReadOnlyList<HistoryMatchDto>> Handle(MatchHistoryPatternsQuery q, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(q.VahedCode)) return [];
        if (await _store.CountAsync(ct) == 0) throw new HistoryPatternsUnavailableException();

        var tokens = HistoryText.Tokens(q.Sentence).Distinct(StringComparer.Ordinal).Take(MaxTokens).ToList();
        if (tokens.Count == 0) return [];

        var found = await _store.FindCandidatesAsync(tokens, q.VahedCode, CandidateTake, ct);
        var set = tokens.ToHashSet(StringComparer.Ordinal);
        var ranked = found.Patterns
            .Select(p => (Pattern: p, Own: found.OwnUnit.GetValueOrDefault(p.Id)))
            .Select(x => (x.Pattern, x.Own, Score: Score(x.Pattern, x.Own, set)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score).ThenByDescending(x => x.Pattern.VoucherCount ?? 0)
            .Take(EvaluateAtMost).ToList();
        if (ranked.Count == 0) return [];

        // فقط معین‌هایی که ماتریس دسترسی کدینگ (ثبت دستی، پیش‌فرض ممنوع) برای این واحد باز کرده است
        var allowed = await _policy.GetManualEntryAllowedIdsAsync(q.VahedCode, DateDoc(q.Year), ct);
        var full = (await _store.GetManyAsync(ranked.Select(r => r.Pattern.Id).ToList(), ct)).ToDictionary(p => p.Id);

        var result = new List<HistoryMatchDto>();
        foreach (var r in ranked)
        {
            if (result.Count >= Top) break;
            if (!full.TryGetValue(r.Pattern.Id, out var pattern)) continue;
            var lines = HistoryJson.Lines(pattern.LinesJson);
            if (lines.Count == 0 || lines.Any(l => !allowed.Contains(l.MoinId))) continue;

            // قاعدهٔ B و معین فعال: الگوی موقت فقط وقتی ساخته می‌شود که واحد برای هر سطح تفصیلی طرف حساب قابل‌انتخاب دارد
            var built = await _factory.BuildAsync(pattern, new HistoryBuildContext(q.VahedCode), ct);
            if (built.Template is not { } template) continue;

            var samples = HistoryJson.Strings(pattern.SampleDescsJson)
                .Select(s => HistoryText.MaskForOtherUnits(s)).Where(s => s.Length > 0)
                .Distinct(StringComparer.Ordinal).Take(3).ToList();
            var mine = r.Own?.VoucherCount ?? 0;
            result.Add(new HistoryMatchDto(pattern.Id, Summary(pattern.VoucherCount ?? 0, mine, samples, built.Keywords),
                samples, pattern.VoucherCount ?? 0, mine, HistoryTemplateFactory.Questions(template)));
        }
        return result;
    }

    internal static double Score(HistoryPattern p, HistoryPatternUnit? own, IReadOnlySet<string> tokens)
    {
        var global = HistoryJson.Keywords(p.KeywordsJson).ToDictionary(k => k.Word, k => k.Count, StringComparer.Ordinal);
        var mine = own is null ? new Dictionary<string, int>() : HistoryJson.Keywords(own.KeywordsJson).ToDictionary(k => k.Word, k => k.Count, StringComparer.Ordinal);
        var gMax = global.Count == 0 ? 1 : Math.Max(1, global.Values.Max());
        var uMax = mine.Count == 0 ? 1 : Math.Max(1, mine.Values.Max());

        double score = 0;
        var matched = 0;
        foreach (var t in tokens)
        {
            var hit = false;
            if (global.TryGetValue(t, out var gc)) { score += 1 + (double)gc / gMax; hit = true; }
            if (mine.TryGetValue(t, out var uc)) { score += 2 * (1 + (double)uc / uMax); hit = true; } // سابقهٔ خود واحد دو برابر
            if (hit) matched++;
        }
        if (matched == 0) return 0;
        score *= 1 + 0.1 * Math.Log(1 + (p.VoucherCount ?? 0));
        return score * ((double)matched / tokens.Count + 0.5);
    }

    private static string Summary(int count, int mine, IReadOnlyList<string> samples, IReadOnlyList<KeywordCount> keywords)
    {
        var shown = samples.Count > 0 ? samples[0] : string.Join(' ', keywords.OrderByDescending(k => k.Count).Take(2).Select(k => k.Word));
        var text = shown.Length > 0
            ? $"در سال‌های قبل {Fa(count)} سند مشابه با شرح «{shown}…» ثبت شده"
            : $"در سال‌های قبل {Fa(count)} سند مشابه ثبت شده";
        return mine > 0 ? $"{text} ({Fa(mine)} مورد در واحد شما)" : text;
    }

    private static string Fa(int n) =>
        string.Concat(n.ToString(CultureInfo.InvariantCulture).Select(c => (char)('۰' + (c - '0'))));

    /// <summary>تاریخ برای ماتریس دسترسی کدینگ: امروز، یا (اگر سال مالی دیگری انتخاب شده) میانهٔ همان سال.</summary>
    private static string DateDoc(string? year)
    {
        var pc = new PersianCalendar();
        var now = DateTime.Now;
        var current = $"{pc.GetYear(now):0000}{pc.GetMonth(now):00}{pc.GetDayOfMonth(now):00}";
        return year is { Length: 4 } y && y.All(char.IsAsciiDigit) && y != current[..4] ? y + "0701" : current;
    }
}
