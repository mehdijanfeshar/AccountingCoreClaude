using System.Text.Json.Serialization;
using Accounting.Application.Common.Exceptions;
using Accounting.Application.Common.Interfaces;
using Accounting.Application.Common.Security;
using Accounting.Application.FinancialStatements.Engine;
using Accounting.Domain.Entity;
using FluentValidation;
using MediatR;

namespace Accounting.Application.FinancialStatements.Consolidation;

/// <summary>
/// تطبیق حساب‌های فی‌مابین به تفکیک جفت واحد (سند منبع صفحهٔ «تلفیق و حذف فی‌مابین»). برای هر قاعدهٔ حذف و هر جفت
/// (واحد الف، واحد ب): ماندهٔ الف روی حساب‌های قاعده با طرف مقابل ب، و ماندهٔ ب با طرف مقابل الف (بدهکار مثبت).
/// <c>Status</c>: ۱ تطبیق، ۲ در آستانهٔ مجاز، ۳ اختلاف، ۴ بدون طرف مقابل، ۵ طرف مقابل نامعلوم (ردیف بی‌شناسه و
/// بی‌اعلامیه). <c>WithRoot</c> = یک سوی جفت واحد صاحب اجراست (مثلاً ستاد ↔ اداره کل).
/// </summary>
public sealed record FsElimPairDto(
    string RuleCode, string RuleTitle, string UnitA, string? NameA, string? UnitB, string? NameB,
    decimal AmountA, decimal AmountB, decimal Difference, int Status, bool WithRoot);

public sealed record FsElimPairsDto(IReadOnlyList<FsElimPairDto> Pairs, decimal? Tolerance, bool Available, string? Message);

public sealed record FsElimPairLineDto(
    Guid VoucherHeadId, string? DocNum, string? DateDoc, string VahedCode, string AccCode, string? Counterparty, string? Source,
    string? AttributeValue, string? LineDesc, decimal Debtor, decimal Creditor);

/// <summary>جفت‌های یک اجرا — از دادهٔ زنده با پارامترهای همان اجرا (دوره پیش از انتشار قفل است، V-11).</summary>
public sealed record GetFsElimPairsQuery(Guid RunId) : IRequest<FsElimPairsDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

/// <summary>ردیف‌های سند پشت یک جفت: ردیف‌های الف با طرف مقابل ب و ردیف‌های ب با طرف مقابل الف (ب خالی = نامعلوم).</summary>
public sealed record GetFsElimPairLinesQuery(Guid RunId, string RuleCode, string UnitA, string? UnitB) : IRequest<IReadOnlyList<FsElimPairLineDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}

public sealed class GetFsElimPairLinesQueryValidator : AbstractValidator<GetFsElimPairLinesQuery>
{
    public GetFsElimPairLinesQueryValidator()
    {
        RuleFor(x => x.RuleCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.UnitA).NotEmpty().MaximumLength(10);
        RuleFor(x => x.UnitB).MaximumLength(10);
    }
}

public static class FsElimPairEngine
{
    /// <summary>
    /// قاعده‌ها به همان ترتیب موتور حذف (<see cref="FsConsolidationEngine.Eliminate"/>): معینی که قاعدهٔ قبلی گرفته به
    /// قاعدهٔ بعدی نمی‌رسد. خروجی: حساب‌های هر قاعده.
    /// </summary>
    public static IReadOnlyList<(TB_FS_ELIM_RULE Rule, HashSet<string> Accounts)> AccountsOf(IReadOnlyList<TB_FS_ELIM_RULE> rules, IReadOnlyList<string> moeins)
    {
        var taken = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<(TB_FS_ELIM_RULE, HashSet<string>)>();
        foreach (var rule in rules)
        {
            if (!AccountSelector.TryParse(rule.LEFT_SELECTOR, out var left, out _) || !AccountSelector.TryParse(rule.RIGHT_SELECTOR, out var right, out _))
            {
                continue;
            }

            var codes = moeins.Where(c => !taken.Contains(c) && (left!.Match(c) is not null || right!.Match(c) is not null))
                .ToHashSet(StringComparer.Ordinal);
            taken.UnionWith(codes);
            result.Add((rule, codes));
        }

        return result;
    }

    public static List<FsElimPairDto> Pairs(
        TB_FS_ELIM_RULE rule, HashSet<string> accounts, IReadOnlyList<FsCounterpartyBalance> balances,
        string root, IReadOnlySet<string> scope, IReadOnlyDictionary<string, string> names)
    {
        var net = new Dictionary<(string Owner, string? Cp), decimal>();
        foreach (var b in balances.Where(b => accounts.Contains(b.AccCode)))
        {
            // طرف مقابلِ خودِ واحد (ردیف داخلی) جفت نمی‌سازد
            var cp = b.Counterparty == b.VahedCode ? null : b.Counterparty;
            var key = (b.VahedCode, cp);
            net[key] = net.GetValueOrDefault(key) + b.Debtor - b.Creditor;
        }

        var result = new List<FsElimPairDto>();
        string? Name(string? c) => c is null ? null : names.GetValueOrDefault(c);

        foreach (var ((owner, cp), amount) in net.Where(x => x.Key.Cp is null && x.Value != 0))
        {
            result.Add(new FsElimPairDto(rule.CODE, rule.TITLE_FA, owner, Name(owner), null, null, amount, 0, amount, 5, owner == root));
        }

        var pairs = net.Keys.Where(k => k.Cp is not null)
            .Select(k => Order(k.Owner, k.Cp!, root))
            .Distinct()
            .ToList();
        foreach (var (a, b) in pairs)
        {
            var x = net.GetValueOrDefault((a, b));
            var y = net.GetValueOrDefault((b, a));
            if (x == 0 && y == 0)
            {
                continue;
            }

            var diff = x + y;
            var status = x == 0 || y == 0 ? 4 : diff == 0 ? 1 : Math.Abs(diff) <= rule.TOLERANCE ? 2 : 3;
            result.Add(new FsElimPairDto(rule.CODE, rule.TITLE_FA, a, Name(a), b, Name(b) ?? (scope.Contains(b) ? null : "بیرون از دامنهٔ اجرا"),
                decimal.Round(x), decimal.Round(y), decimal.Round(diff), status, a == root || b == root));
        }

        return result;
    }

    /// <summary>واحد صاحب اجرا همیشه «الف»؛ وگرنه ترتیب کد.</summary>
    private static (string, string) Order(string x, string y, string root)
        => y == root || (x != root && string.CompareOrdinal(y, x) < 0) ? (y, x) : (x, y);
}

public sealed class FsElimPairHandlers :
    IRequestHandler<GetFsElimPairsQuery, FsElimPairsDto>,
    IRequestHandler<GetFsElimPairLinesQuery, IReadOnlyList<FsElimPairLineDto>>
{
    private readonly IFsConsolidationRepository _repo;
    private readonly IFsRunRepository _runs;
    private readonly IFsBalanceReadRepository _balances;
    private readonly IFsUnitScopeProvider _scopes;

    public FsElimPairHandlers(IFsConsolidationRepository repo, IFsRunRepository runs, IFsBalanceReadRepository balances, IFsUnitScopeProvider scopes)
    {
        _repo = repo;
        _runs = runs;
        _balances = balances;
        _scopes = scopes;
    }

    private sealed record Context(
        Queries.FinancialStatements.FsRunSummaryDto Run, IReadOnlyList<TB_FS_RUN_UNIT> Units,
        IReadOnlyList<(TB_FS_ELIM_RULE Rule, HashSet<string> Accounts)> Rules);

    /// <summary>پارامترهای اجرا، واحدهای Snapshot و قواعد همان‌طور که در تهیهٔ اجرا انتخاب شدند (نزدیک‌ترین مالک).</summary>
    private async Task<Context?> LoadAsync(Guid runId, string vahedCode, CancellationToken ct)
    {
        var detail = await _runs.GetDetailAsync(runId, vahedCode, ct) ?? throw new NotFoundException("FsRun", runId);
        if (!detail.Run.IncludeSubUnits)
        {
            return null;
        }

        var scope = await _scopes.GetAsync(vahedCode, ct);
        var units = (await _repo.GetRunUnitsAsync(runId, vahedCode, ct)).Where(u => u.KIND == 1).ToList();
        var rules = (await _repo.GetElimRulesAsync(detail.Run.Framework, ct))
            .Where(r => r.IS_ACTIVE)
            .Select(r => (Rule: r, Priority: scope.PriorityOf(r.VAHEDCODE)))
            .Where(x => x.Priority is not null)
            .GroupBy(x => x.Rule.CODE, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderBy(x => x.Priority).First().Rule)
            .OrderBy(r => r.CODE, StringComparer.Ordinal)
            .ToList();
        var moeins = await _balances.GetMoeinCodesAsync(ct);
        return new Context(detail.Run, units, FsElimPairEngine.AccountsOf(rules, moeins));
    }

    private static string ToDate(Queries.FinancialStatements.FsRunSummaryDto run) => $"{run.Year}{run.ToMonth:00}31";

    public async Task<FsElimPairsDto> Handle(GetFsElimPairsQuery request, CancellationToken cancellationToken)
    {
        var ctx = await LoadAsync(request.RunId, request.VahedCode, cancellationToken);
        if (ctx is null)
        {
            return new FsElimPairsDto([], null, false, "این اجرا صورت ترکیبی زیرواحدها نیست؛ تطبیق جفت واحدها ندارد.");
        }

        if (ctx.Rules.Count == 0)
        {
            return new FsElimPairsDto([], null, false, "قاعدهٔ حذف فی‌مابین فعالی تعریف نشده است (صفحهٔ «تلفیق و XBRL» ← حذف فی‌مابین).");
        }

        var unitCodes = ctx.Units.Select(u => u.UNIT_CODE).ToList();
        var balances = await _balances.GetCounterpartyBalancesAsync(
            ctx.Run.Year, $"{ctx.Run.Year}0101", ToDate(ctx.Run), unitCodes, ctx.Run.MinDocLife,
            ctx.Rules.SelectMany(r => r.Accounts).Distinct().ToList(), cancellationToken);

        var scope = unitCodes.ToHashSet(StringComparer.Ordinal);
        var names = ctx.Units.GroupBy(u => u.UNIT_CODE).ToDictionary(g => g.Key, g => g.First().UNIT_NAME ?? g.Key);
        var pairs = ctx.Rules
            .SelectMany(r => FsElimPairEngine.Pairs(r.Rule, r.Accounts, balances, ctx.Run.VahedCode, scope, names))
            .OrderBy(p => p.Status switch { 3 => 0, 4 => 1, 5 => 2, 2 => 3, _ => 4 })
            .ThenByDescending(p => Math.Abs(p.Difference))
            .ToList();
        var tolerance = ctx.Rules.Select(r => r.Rule.TOLERANCE).Distinct().Count() == 1 ? ctx.Rules[0].Rule.TOLERANCE : (decimal?)null;
        return new FsElimPairsDto(pairs, tolerance, true, null);
    }

    public async Task<IReadOnlyList<FsElimPairLineDto>> Handle(GetFsElimPairLinesQuery request, CancellationToken cancellationToken)
    {
        var ctx = await LoadAsync(request.RunId, request.VahedCode, cancellationToken);
        var rule = ctx?.Rules.FirstOrDefault(r => r.Rule.CODE.Equals(request.RuleCode, StringComparison.OrdinalIgnoreCase));
        if (ctx is null || rule?.Rule is null)
        {
            return [];
        }

        // فقط واحدهای همین اجرا (و دامنهٔ کاربر از VahedScope) — نه هر کد واحدی که در درخواست آمده
        var inRun = ctx.Units.Select(u => u.UNIT_CODE).ToHashSet(StringComparer.Ordinal);
        if (!inRun.Contains(request.UnitA))
        {
            return [];
        }

        var accounts = rule.Value.Accounts.ToList();
        var lines = (await _balances.GetCounterpartyLinesAsync(ctx.Run.Year, ToDate(ctx.Run), ctx.Run.MinDocLife, accounts,
            request.UnitA, request.UnitB, cancellationToken)).ToList();
        if (request.UnitB is { } b && inRun.Contains(b))
        {
            lines.AddRange(await _balances.GetCounterpartyLinesAsync(ctx.Run.Year, ToDate(ctx.Run), ctx.Run.MinDocLife, accounts,
                b, request.UnitA, cancellationToken));
        }

        return lines.Select(l => new FsElimPairLineDto(l.VoucherHeadId, l.DocNum, l.DateDoc, l.VahedCode, l.AccCode, l.Counterparty,
            l.Source, l.AttributeValue, l.LineDesc, l.Debtor, l.Creditor)).ToList();
    }
}
