using Accounting.Application.FinancialStatements.Engine;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.FinancialStatements.Consolidation;

// ط-۴/ط-۵ (docs/fs-module.md §۱۴) — منطق خالص تلفیق: تسعیر تراز شرکت تابعه، سهم غیرکنترلی و حذف فی‌مابین.
// ورودی و خروجی همان FsAccountBalance موتور است تا صورت‌ها بدون تغییر موتور محاسبه شوند.

/// <summary>کدهای معین مصنوعی که تلفیق می‌سازد؛ در انتخاب‌گر قالب مثل معین عادی به‌کار می‌روند.</summary>
public static class FsSyntheticAccounts
{
    /// <summary>ذخیرهٔ تسعیر ارز (اختلاف تسعیر شرکت‌های تابعه).</summary>
    public const string TranslationReserve = "FXR";

    /// <summary>سهم غیرکنترلی (بستانکار).</summary>
    public const string NonControllingInterest = "NCI";

    /// <summary>طرف مقابل سهم غیرکنترلی (بدهکار) — معمولاً به کاهش سهم مالکان واحد تجاری اصلی نگاشت می‌شود.</summary>
    public const string NonControllingOffset = "NCIOFF";

    /// <summary>گروه ستون «حذفیات» در کاربرگ.</summary>
    public const string EliminationGroup = "ELIM";

    public static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>
    {
        [TranslationReserve] = "ذخیرهٔ تسعیر ارز",
        [NonControllingInterest] = "سهم غیرکنترلی",
        [NonControllingOffset] = "سهم غیرکنترلی — طرف مقابل",
    };
}

public sealed record FsEntityInput(TB_FS_ENTITY Entity, IReadOnlyList<TB_FS_ENTITY_TB> Rows, TB_FS_ENTITY_RATE? Rate);

/// <summary>نتیجهٔ یک قاعدهٔ حذف. Status: ۱ تطبیق، ۲ در آستانه، ۳ عدم تطبیق، ۴ طرف مقابل ندارد.</summary>
public sealed record FsElimResult(string Code, string TitleFa, decimal Left, decimal Right, decimal Difference, int Status);

public static class FsConsolidationEngine
{
    public const int AssetLiability = 1;
    public const int Equity = 2;
    public const int ProfitLoss = 3;

    /// <summary>ارز ریالی (یا خالی) نرخ ندارد.</summary>
    public static bool IsLocalCurrency(string? currency) => string.IsNullOrWhiteSpace(currency) || currency.Equals("IRR", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// تراز شرکت تابعه به ریال (استاندارد ۱۶): دارایی/بدهی — ابتدا با نرخ ابتدا، پایان با نرخ پایان؛ حقوق مالکانه —
    /// ابتدا با نرخ ابتدا و گردش با نرخ میانگین (تاریخی)؛ سود و زیان — نرخ میانگین. اختلاف ⇒ <c>FXR</c>. سهم غیرکنترلی =
    /// (۱ − مالکیت) × خالص دارایی‌ها ⇒ <c>NCI</c> بستانکار و <c>NCIOFF</c> بدهکار. واحد هر مانده = کد شرکت.
    /// </summary>
    public static IReadOnlyList<FsAccountBalance> Translate(FsEntityInput input)
    {
        var e = input.Entity;
        var local = IsLocalCurrency(e.CURRENCY);
        decimal open = local ? 1 : input.Rate?.OPENING_RATE ?? 0;
        decimal close = local ? 1 : input.Rate?.CLOSING_RATE ?? 0;
        decimal avg = local ? 1 : input.Rate?.AVERAGE_RATE ?? 0;

        if (!local && (open <= 0 || close <= 0 || avg <= 0))
        {
            throw new FsEngineException($"نرخ تسعیر شرکت «{e.TITLE_FA}» ({e.CURRENCY}) برای این دوره کامل وارد نشده است.");
        }

        var result = new List<FsAccountBalance>();
        decimal openSum = 0, closeSum = 0, netOpen = 0, netClose = 0;

        foreach (var r in input.Rows)
        {
            var openFc = r.OPENING_DEBTOR - r.OPENING_CREDITOR;
            var periodFc = r.PERIOD_DEBTOR - r.PERIOD_CREDITOR;

            decimal tOpen, tClose;

            switch (r.ACC_CLASS)
            {
                case ProfitLoss:
                    tOpen = openFc * avg;
                    tClose = tOpen + periodFc * avg;
                    break;
                case Equity:
                    tOpen = openFc * open;
                    tClose = tOpen + periodFc * avg;
                    break;
                default:
                    tOpen = openFc * open;
                    tClose = (openFc + periodFc) * close;
                    netOpen += tOpen;
                    netClose += tClose;
                    break;
            }

            openSum += tOpen;
            closeSum += tClose;
            result.Add(Balance(r.ACCCODE, r.SOURCE_ACCNAME, tOpen, tClose - tOpen, e.CODE));
        }

        // اختلاف تسعیر: ترازِ به‌ریال‌شده باید صفر شود.
        if (Math.Round(openSum, 2) != 0 || Math.Round(closeSum - openSum, 2) != 0)
        {
            result.Add(Balance(FsSyntheticAccounts.TranslationReserve, FsSyntheticAccounts.Names[FsSyntheticAccounts.TranslationReserve], -openSum, -(closeSum - openSum), e.CODE));
        }

        var share = 1m - Math.Clamp(e.OWNERSHIP, 0, 100) / 100m;

        if (share > 0 && (netOpen != 0 || netClose != 0))
        {
            var nciOpen = share * netOpen;
            var nciPeriod = share * (netClose - netOpen);
            result.Add(Balance(FsSyntheticAccounts.NonControllingInterest, FsSyntheticAccounts.Names[FsSyntheticAccounts.NonControllingInterest], -nciOpen, -nciPeriod, e.CODE));
            result.Add(Balance(FsSyntheticAccounts.NonControllingOffset, FsSyntheticAccounts.Names[FsSyntheticAccounts.NonControllingOffset], nciOpen, nciPeriod, e.CODE));
        }

        return result;
    }

    /// <summary>
    /// حذف فی‌مابین (سند منبع §۸): برای هر قاعده جمع مانده‌های پایان دو سو؛ هر دو سو با مانده‌های قرینه در گروه
    /// <c>ELIM</c> صفر می‌شوند. معینی که قاعدهٔ قبلی حذفش کرده دوباره حذف نمی‌شود. اختلاف = جمع دو سو.
    /// </summary>
    public static (IReadOnlyList<FsAccountBalance> Adjustments, IReadOnlyList<FsElimResult> Results) Eliminate(
        IReadOnlyList<FsAccountBalance> balances, IReadOnlyList<TB_FS_ELIM_RULE> rules)
    {
        var adjustments = new List<FsAccountBalance>();
        var results = new List<FsElimResult>();
        var eliminated = new HashSet<string>(StringComparer.Ordinal);
        var codes = balances.Select(b => b.AccCode).Distinct(StringComparer.Ordinal).ToList();

        foreach (var rule in rules)
        {
            if (!AccountSelector.TryParse(rule.LEFT_SELECTOR, out var left, out _) || !AccountSelector.TryParse(rule.RIGHT_SELECTOR, out var right, out _))
            {
                results.Add(new FsElimResult(rule.CODE, rule.TITLE_FA, 0, 0, 0, 3));
                continue;
            }

            var leftCodes = codes.Where(c => !eliminated.Contains(c) && left!.Match(c) is not null).ToHashSet(StringComparer.Ordinal);
            var rightCodes = codes.Where(c => !eliminated.Contains(c) && !leftCodes.Contains(c) && right!.Match(c) is not null).ToHashSet(StringComparer.Ordinal);

            decimal Sum(HashSet<string> set) => balances.Where(b => set.Contains(b.AccCode)).Sum(Closing);
            var l = Sum(leftCodes);
            var r = Sum(rightCodes);

            if (l == 0 && r == 0)
            {
                continue;
            }

            var diff = l + r;
            var status = l == 0 || r == 0 ? 4 : diff == 0 ? 1 : Math.Abs(diff) <= rule.TOLERANCE ? 2 : 3;
            results.Add(new FsElimResult(rule.CODE, rule.TITLE_FA, decimal.Round(l), decimal.Round(r), decimal.Round(diff), status));

            foreach (var b in balances.Where(b => leftCodes.Contains(b.AccCode) || rightCodes.Contains(b.AccCode)))
            {
                adjustments.Add(new FsAccountBalance(
                    b.AccCode, b.AccName, -b.OpeningDebtor, -b.OpeningCreditor, -b.PeriodDebtor, -b.PeriodCreditor, FsSyntheticAccounts.EliminationGroup));
            }

            eliminated.UnionWith(leftCodes);
            eliminated.UnionWith(rightCodes);
        }

        return (adjustments, results);
    }

    /// <summary>مقدار تنظیم برای واحد: نزدیک‌ترین مالک (خود، والد، …، مشترک).</summary>
    public static string? Setting(IEnumerable<TB_FS_SETTING> settings, FsUnitScope scope, FsFramework framework, string key)
        => settings
            .Where(s => s.FRAMEWORK == framework && s.SETTING_KEY == key && !string.IsNullOrWhiteSpace(s.SETTING_VALUE))
            .Select(s => (s, p: scope.PriorityOf(s.VAHEDCODE)))
            .Where(x => x.p is not null)
            .OrderBy(x => x.p)
            .Select(x => x.s.SETTING_VALUE)
            .FirstOrDefault();

    private static decimal Closing(FsAccountBalance b) => b.OpeningDebtor - b.OpeningCreditor + b.PeriodDebtor - b.PeriodCreditor;

    private static FsAccountBalance Balance(string code, string? name, decimal openingNet, decimal periodNet, string unit)
        => new(
            code,
            name,
            openingNet > 0 ? openingNet : 0,
            openingNet < 0 ? -openingNet : 0,
            periodNet > 0 ? periodNet : 0,
            periodNet < 0 ? -periodNet : 0,
            unit);
}
