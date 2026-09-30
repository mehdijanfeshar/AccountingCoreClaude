using System.Globalization;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.FinancialStatements.Engine;

/// <summary>قاعدهٔ داده‌ای تساوی (از <c>TB_FS_CHECK_RULE</c>): <c>Left = Right</c> با اختلاف مجاز.</summary>
public sealed record FsCheckRuleInput(string Code, string TitleFa, string LeftExpr, string RightExpr, decimal Tolerance, FsCheckSeverity Severity);

/// <summary>یک صورت/یادداشت اجرا برای کنترل‌ها.</summary>
public sealed record FsCheckStatement(string TemplateCode, string TitleFa, bool IsNote, IReadOnlyList<FsCheckStatementRow> Rows);

public sealed record FsCheckStatementRow(string Code, string? TitleFa, FsRowType RowType, string? Selector, FsValueType? ValueType, FsNormalBalance? NormalBalance);

/// <summary>نتیجهٔ کنترل یادداشت V-08 که هنگام Snapshot حساب شد.</summary>
public sealed record FsNoteCheckInput(string NoteCode, string? NoteNo, string TitleFa, string? ParentRef, decimal? DiffCur, decimal? DiffPrv);

public sealed record FsCheckResult(
    string Code,
    string TitleFa,
    FsCheckSeverity Severity,
    bool Passed,
    string? Message,
    decimal? Difference,
    string? RowRef);

/// <summary>
/// کنترل‌های یک اجرا (بخش ۴۵-ه، سند منبع §۱۰) — خالص:
/// <list type="bullet">
/// <item><b>V-01</b> تراز آزمایشی دامنه متوازن است (جمع بدهکار = جمع بستانکار) — مسدودکننده.</item>
/// <item><b>قواعد داده‌ای</b> (V-02..V-04 پیش‌فرض و هر قاعدهٔ تعریف‌شده): <c>Left = Right</c> با <c>STMT</c>، ستون جاری.</item>
/// <item><b>V-05</b> معین دارای مانده که در هیچ ردیف صورتی نیامده، یا در یک صورت در دو ردیف آمده — هشدار.</item>
/// <item><b>V-06</b> ماندهٔ معکوس: ردیف مانده‌پایانِ بدهکار با مبلغ بستانکار یا برعکس — هشدار.</item>
/// <item><b>V-08</b> جمع یادداشت = ردیف صورت (از Snapshot) — مسدودکننده.</item>
/// </list>
/// </summary>
public static class FsRunChecks
{
    private const int MaxListed = 10;

    public static IReadOnlyList<FsCheckResult> Evaluate(
        IReadOnlyList<FsCheckStatement> statements,
        IReadOnlyDictionary<(string Stmt, string Row), IReadOnlyDictionary<string, FsRowValue>> values,
        IReadOnlyList<FsAccountBalance> currentBalances,
        IReadOnlyList<FsCheckRuleInput> rules,
        IReadOnlyList<FsNoteCheckInput> notes)
    {
        var results = new List<FsCheckResult>();

        // V-01
        var dr = currentBalances.Sum(b => b.OpeningDebtor + b.PeriodDebtor);
        var cr = currentBalances.Sum(b => b.OpeningCreditor + b.PeriodCreditor);
        results.Add(new FsCheckResult(
            "V-01", "تراز آزمایشی واحدهای دامنه متوازن است", FsCheckSeverity.Blocking, dr == cr,
            dr == cr ? null : $"جمع بدهکار و بستانکار اسناد دامنه {Num(dr - cr)} ریال اختلاف دارد.",
            dr - cr, null));

        // قواعد داده‌ای
        foreach (var rule in rules)
        {
            results.Add(EvaluateRule(rule, values));
        }

        var main = statements.Where(s => !s.IsNote).ToList();
        CheckCoverage(main, currentBalances, results);
        CheckReverseBalances(main, values, results);

        // V-08
        foreach (var n in notes.Where(n => n.ParentRef is not null))
        {
            var diffs = new[] { n.DiffCur, n.DiffPrv }.Where(d => d.HasValue).Select(d => d!.Value).ToList();
            var bad = diffs.FirstOrDefault(d => d != 0);
            results.Add(new FsCheckResult(
                "V-08",
                $"جمع یادداشت {n.NoteNo} ({n.TitleFa}) = ردیف صورت",
                FsCheckSeverity.Blocking,
                bad == 0,
                bad == 0 ? null : $"اختلاف {Num(bad)} ریال با ردیف {n.ParentRef}.",
                bad == 0 ? null : bad,
                n.NoteCode));
        }

        return results;
    }

    /// <summary>
    /// ارزیابی یک قاعدهٔ تساوی روی ستون جاری. فقط عدد، <c>STMT</c>، عملگرها و <c>ABS/ROUND/IF</c> مجازند
    /// (ارجاع مستقیم به ردیف معنا ندارد چون قاعده به صورت خاصی تعلق ندارد).
    /// </summary>
    public static FsCheckResult EvaluateRule(
        FsCheckRuleInput rule,
        IReadOnlyDictionary<(string Stmt, string Row), IReadOnlyDictionary<string, FsRowValue>> values)
    {
        try
        {
            var left = Eval(FsFormula.Parse(rule.LeftExpr).Root, values);
            var right = Eval(FsFormula.Parse(rule.RightExpr).Root, values);
            var diff = left - right;
            var passed = Math.Abs(diff) <= rule.Tolerance;

            return new FsCheckResult(
                rule.Code, rule.TitleFa, rule.Severity, passed,
                passed ? null : $"سمت چپ {Num(left)} و سمت راست {Num(right)} ریال؛ اختلاف {Num(diff)}.",
                passed ? null : diff,
                null);
        }
        catch (Exception ex) when (ex is FsExpressionException or FsEngineException)
        {
            return new FsCheckResult(rule.Code, rule.TitleFa, FsCheckSeverity.Warning, false,
                "کنترل اجرا نشد: " + ex.Message, null, null);
        }
    }

    /// <summary>عبارت قاعده برای بررسی نحو هنگام ذخیرهٔ قاعده: فقط گره‌های مجاز.</summary>
    public static bool IsValidRuleExpression(string? text, out string? error)
    {
        if (!FsFormula.TryParse(text, out var f, out error))
        {
            return false;
        }

        var bad = FsFormula.Walk(f!.Root).FirstOrDefault(e => e is FsRowRefExpr or FsSumRangeExpr or FsPriorExpr);

        if (bad is not null)
        {
            error = "در قاعدهٔ کنترل فقط STMT(قالب, ردیف)، عدد و عملگرها مجازند؛ ارجاع مستقیم به ردیف یا SUM/PRIOR نه.";
            return false;
        }

        return true;
    }

    private static decimal Eval(FsExpr e, IReadOnlyDictionary<(string Stmt, string Row), IReadOnlyDictionary<string, FsRowValue>> values)
    {
        decimal Sub(FsExpr x) => Eval(x, values);

        return e switch
        {
            FsNumberExpr n => n.Value,
            FsStatementRefExpr s => values.TryGetValue((s.TemplateCode, s.RowCode), out var byCol)
                ? byCol.GetValueOrDefault(FsColumns.Current)?.Amount ?? 0m
                : throw new FsEngineException($"صورت/ردیف «{s.TemplateCode} / {s.RowCode}» در این اجرا نیست."),
            FsUnaryExpr u => -Sub(u.Operand),
            FsBinaryExpr { Op: '+' } b => Sub(b.Left) + Sub(b.Right),
            FsBinaryExpr { Op: '-' } b => Sub(b.Left) - Sub(b.Right),
            FsBinaryExpr { Op: '*' } b => Sub(b.Left) * Sub(b.Right),
            FsBinaryExpr { Op: '/' } b => Sub(b.Right) is var r && r == 0 ? 0 : Sub(b.Left) / r,
            FsFunctionExpr { Name: "ABS" } f => Math.Abs(Sub(f.Args[0])),
            FsFunctionExpr { Name: "ROUND" } f => Math.Round(Sub(f.Args[0]), (int)((FsNumberExpr)f.Args[1]).Value, MidpointRounding.AwayFromZero),
            FsIfExpr i => (i.CompareOp switch
            {
                "=" => Sub(i.CompareLeft) == Sub(i.CompareRight),
                "<>" => Sub(i.CompareLeft) != Sub(i.CompareRight),
                "<" => Sub(i.CompareLeft) < Sub(i.CompareRight),
                "<=" => Sub(i.CompareLeft) <= Sub(i.CompareRight),
                ">" => Sub(i.CompareLeft) > Sub(i.CompareRight),
                ">=" => Sub(i.CompareLeft) >= Sub(i.CompareRight),
                _ => false,
            }) ? Sub(i.Then) : Sub(i.Else),
            _ => throw new FsEngineException("در قاعدهٔ کنترل فقط STMT، عدد و عملگرها مجازند."),
        };
    }

    /// <summary>V-05: پوشش معین‌ها در صورت‌های اصلی (نه یادداشت‌ها).</summary>
    private static void CheckCoverage(IReadOnlyList<FsCheckStatement> main, IReadOnlyList<FsAccountBalance> balances, List<FsCheckResult> results)
    {
        var selectors = main
            .SelectMany(s => s.Rows
                .Where(r => r.RowType == FsRowType.Account && r.Selector is not null)
                .Select(r => (Stmt: s, Row: r, Selector: AccountSelector.TryParse(r.Selector, out var sel, out _) ? sel : null)))
            .Where(x => x.Selector is not null)
            .ToList();

        var unmapped = new List<string>();
        var doubled = new List<string>();

        foreach (var b in balances)
        {
            var closing = b.OpeningDebtor + b.PeriodDebtor - b.OpeningCreditor - b.PeriodCreditor;
            var movement = b.PeriodDebtor - b.PeriodCreditor;

            if (closing == 0 && movement == 0)
            {
                continue;
            }

            var matches = selectors
                .Select(x => (x.Stmt, x.Row, Term: x.Selector!.Match(b.AccCode)))
                .Where(x => x.Term is not null)
                .ToList();

            if (matches.Count == 0)
            {
                unmapped.Add(b.AccCode);
                continue;
            }

            // دو ردیف در یک صورت، هر دو بدون [D]/[C] — تقسیم عمدی مانده بدهکار/بستانکار دوبار شمردن نیست.
            foreach (var g in matches.Where(m => m.Term!.Side == SelectorBalanceSide.Any).GroupBy(m => m.Stmt.TemplateCode).Where(g => g.Count() > 1))
            {
                doubled.Add($"{b.AccCode} ({g.First().Stmt.TitleFa}: {string.Join("، ", g.Select(m => m.Row.Code))})");
            }
        }

        results.Add(new FsCheckResult(
            "V-05", "هر معین دارای مانده در صورت‌ها آمده است", FsCheckSeverity.Warning, unmapped.Count == 0,
            unmapped.Count == 0 ? null : $"{unmapped.Count} معین در هیچ ردیفی نیامده: {List(unmapped)}", null, null));

        if (doubled.Count > 0)
        {
            results.Add(new FsCheckResult(
                "V-05", "هیچ معینی در یک صورت دوبار شمرده نشده", FsCheckSeverity.Warning, false,
                $"{doubled.Count} مورد: {List(doubled)}", null, null));
        }
    }

    /// <summary>V-06: ماندهٔ معکوس در ردیف‌های «مانده پایان».</summary>
    private static void CheckReverseBalances(
        IReadOnlyList<FsCheckStatement> main,
        IReadOnlyDictionary<(string Stmt, string Row), IReadOnlyDictionary<string, FsRowValue>> values,
        List<FsCheckResult> results)
    {
        var reversed = new List<(string Label, string Ref, decimal Amount)>();

        foreach (var s in main)
        {
            foreach (var r in s.Rows.Where(r => r.RowType == FsRowType.Account && (r.ValueType ?? FsValueType.Closing) == FsValueType.Closing))
            {
                var amount = values.TryGetValue((s.TemplateCode, r.Code), out var byCol) ? byCol.GetValueOrDefault(FsColumns.Current)?.Amount ?? 0 : 0;

                if ((r.NormalBalance == FsNormalBalance.Debit && amount < 0) || (r.NormalBalance == FsNormalBalance.Credit && amount > 0))
                {
                    reversed.Add(($"{r.Code} {r.TitleFa}", $"{s.TemplateCode}/{r.Code}", amount));
                }
            }
        }

        results.Add(new FsCheckResult(
            "V-06", "ماندهٔ معکوس در ردیف‌های دارایی/بدهی نیست", FsCheckSeverity.Warning, reversed.Count == 0,
            reversed.Count == 0 ? null : $"{reversed.Count} ردیف: {List(reversed.Select(x => x.Label).ToList())}",
            null,
            reversed.Count == 1 ? reversed[0].Ref : null));
    }

    private static string List(IReadOnlyList<string> items)
        => string.Join("، ", items.Take(MaxListed)) + (items.Count > MaxListed ? $" و {items.Count - MaxListed} مورد دیگر" : string.Empty);

    private static string Num(decimal v) => v.ToString("#,##0", CultureInfo.InvariantCulture);
}
