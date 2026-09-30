using Accounting.Domain.ValueObjects;

namespace Accounting.Application.FinancialStatements.Engine;

public enum FsIssueSeverity
{
    Warning = 1,
    Error = 2,
}

/// <summary>یک یافتهٔ اعتبارسنجی قالب. <see cref="RowCode"/> خالی یعنی یافتهٔ سطح نسخه.</summary>
public sealed record FsTemplateIssue(string? RowCode, string Field, FsIssueSeverity Severity, string Message);

/// <summary>ورودی بررسی — یک ردیف نسخه، مستقل از Entity تا در تست و seed هم قابل استفاده باشد.</summary>
public sealed record FsCheckRow(
    string Code,
    string? ParentCode,
    int OrderNo,
    FsRowType RowType,
    string? TitleFa,
    FsNormalBalance? NormalBalance,
    string? Selector,
    FsValueType? ValueType,
    string? Formula);

/// <summary>
/// اعتبارسنجی کامل یک نسخهٔ قالب (سند منبع §۷-۳ و §۱۲-۳ «اعتبارسنجی قالب») — پیش از فعال‌سازی و در
/// endpoint <c>validate</c> اجرا می‌شود. خالص و بدون وابستگی به دیتابیس:
/// <list type="bullet">
/// <item>کد ردیف معتبر و یکتا؛ والد موجود و از نوع Header؛</item>
/// <item>فیلدهای لازم هر نوع ردیف و نحو انتخاب‌گر/فرمول؛</item>
/// <item>ارجاع فرمول‌ها به ردیف مقداری موجود، و دو سرِ <c>SUM</c> به ترتیب درست؛</item>
/// <item><c>STMT</c> فقط به قالب موجود دیگر (اگر <c>knownTemplateCodes</c> داده شود)؛</item>
/// <item>نبودِ ارجاع دوری (با بازکردن <c>SUM</c> به ردیف‌های بازه).</item>
/// </list>
/// پوشش حساب‌ها (معین بدون ردیف / معین در دو ردیف) به کدینگ واقعی نیاز دارد و در بخش ۴۵-ه
/// (صفحهٔ نگاشت حساب‌ها) اضافه می‌شود.
/// </summary>
public static class FsTemplateChecker
{
    public static IReadOnlyList<FsTemplateIssue> Check(
        IReadOnlyList<FsCheckRow> rows,
        string? ownTemplateCode = null,
        IReadOnlySet<string>? knownTemplateCodes = null,
        string? noteTotalRowCode = null)
    {
        var issues = new List<FsTemplateIssue>();

        if (rows.Count == 0)
        {
            issues.Add(new FsTemplateIssue(null, "Rows", FsIssueSeverity.Error, "نسخه هیچ ردیفی ندارد."));
            return issues;
        }

        var ordered = rows.OrderBy(r => r.OrderNo).ToList();
        var byCode = new Dictionary<string, FsCheckRow>(StringComparer.Ordinal);

        foreach (var r in ordered)
        {
            if (!FsText.IsValidRowCode(r.Code))
            {
                issues.Add(new FsTemplateIssue(r.Code, "Code", FsIssueSeverity.Error,
                    $"کد ردیف «{r.Code}» نامعتبر است (حرف لاتین + حداکثر ۱۹ حرف/رقم/زیرخط، و نه نام تابع)."));
            }

            if (!byCode.TryAdd(r.Code, r))
            {
                issues.Add(new FsTemplateIssue(r.Code, "Code", FsIssueSeverity.Error, $"کد ردیف «{r.Code}» تکراری است."));
            }
        }

        var formulas = new Dictionary<string, FsFormula>(StringComparer.Ordinal);

        foreach (var r in ordered)
        {
            CheckParent(r, byCode, issues);
            CheckFields(r, issues, formulas);
        }

        var valueRowIndex = ordered
            .Select((r, i) => (r, i))
            .ToDictionary(x => x.r.Code, x => x.i, StringComparer.Ordinal);

        foreach (var (code, f) in formulas)
        {
            foreach (var refCode in f.DirectRowRefs().Distinct(StringComparer.Ordinal))
            {
                if (!byCode.TryGetValue(refCode, out var target))
                {
                    issues.Add(new FsTemplateIssue(code, "Formula", FsIssueSeverity.Error, $"فرمول به ردیف ناموجود «{refCode}» ارجاع می‌دهد."));
                }
                else if (!IsValueRow(target.RowType))
                {
                    issues.Add(new FsTemplateIssue(code, "Formula", FsIssueSeverity.Error,
                        $"فرمول به ردیف «{refCode}» ارجاع می‌دهد که مقدار ندارد (عنوان/متن/خالی)."));
                }
            }

            foreach (var s in f.SumRanges())
            {
                if (valueRowIndex.TryGetValue(s.FromRowCode, out var a) && valueRowIndex.TryGetValue(s.ToRowCode, out var b) && a > b)
                {
                    issues.Add(new FsTemplateIssue(code, "Formula", FsIssueSeverity.Error,
                        $"در SUM({s.FromRowCode}:{s.ToRowCode}) ردیف اول پس از ردیف دوم آمده است."));
                }
            }

            foreach (var st in f.StatementRefs())
            {
                if (!FsText.IsValidTemplateCode(st.TemplateCode))
                {
                    issues.Add(new FsTemplateIssue(code, "Formula", FsIssueSeverity.Error, $"کد قالب «{st.TemplateCode}» در STMT نامعتبر است."));
                }
                else if (ownTemplateCode is not null && string.Equals(st.TemplateCode, ownTemplateCode, StringComparison.Ordinal))
                {
                    issues.Add(new FsTemplateIssue(code, "Formula", FsIssueSeverity.Error,
                        "STMT برای ارجاع به صورت دیگر است؛ برای ردیف همین صورت، کد ردیف را مستقیم بنویسید."));
                }
                else if (knownTemplateCodes is not null && !knownTemplateCodes.Contains(st.TemplateCode))
                {
                    issues.Add(new FsTemplateIssue(code, "Formula", FsIssueSeverity.Error, $"قالب «{st.TemplateCode}» وجود ندارد."));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(noteTotalRowCode))
        {
            if (!byCode.TryGetValue(noteTotalRowCode, out var totalRow))
            {
                issues.Add(new FsTemplateIssue(null, "NoteTotalRowCode", FsIssueSeverity.Error,
                    $"ردیف جمع یادداشت «{noteTotalRowCode}» در این نسخه وجود ندارد."));
            }
            else if (!IsValueRow(totalRow.RowType))
            {
                issues.Add(new FsTemplateIssue(noteTotalRowCode, "NoteTotalRowCode", FsIssueSeverity.Error,
                    "ردیف جمع یادداشت باید ردیف مقداری (حساب، فرمول یا دستی) باشد."));
            }
        }

        if (!issues.Any(i => i.Severity == FsIssueSeverity.Error))
        {
            CheckCycles(ordered, formulas, valueRowIndex, issues);
        }

        return issues;
    }

    private static bool IsValueRow(FsRowType t) => t is FsRowType.Account or FsRowType.Formula or FsRowType.External;

    private static void CheckParent(FsCheckRow r, Dictionary<string, FsCheckRow> byCode, List<FsTemplateIssue> issues)
    {
        if (r.ParentCode is null)
        {
            return;
        }

        if (!byCode.TryGetValue(r.ParentCode, out var parent))
        {
            issues.Add(new FsTemplateIssue(r.Code, "Parent", FsIssueSeverity.Error, $"ردیف والد «{r.ParentCode}» وجود ندارد."));
        }
        else if (parent.RowType != FsRowType.Header)
        {
            issues.Add(new FsTemplateIssue(r.Code, "Parent", FsIssueSeverity.Error, $"والد «{r.ParentCode}» باید از نوع «عنوان» باشد."));
        }
        else if (parent.OrderNo > r.OrderNo)
        {
            issues.Add(new FsTemplateIssue(r.Code, "Parent", FsIssueSeverity.Warning, $"والد «{r.ParentCode}» پس از این ردیف آمده است."));
        }
    }

    private static void CheckFields(FsCheckRow r, List<FsTemplateIssue> issues, Dictionary<string, FsFormula> formulas)
    {
        if (r.RowType is FsRowType.Header or FsRowType.Account or FsRowType.Formula or FsRowType.External
            && string.IsNullOrWhiteSpace(r.TitleFa))
        {
            issues.Add(new FsTemplateIssue(r.Code, "TitleFa", FsIssueSeverity.Warning, "عنوان فارسی خالی است."));
        }

        switch (r.RowType)
        {
            case FsRowType.Account:
                if (string.IsNullOrWhiteSpace(r.Selector))
                {
                    issues.Add(new FsTemplateIssue(r.Code, "Selector", FsIssueSeverity.Error, "ردیف حساب به انتخاب‌گر حساب نیاز دارد."));
                }
                else if (!AccountSelector.TryParse(r.Selector, out _, out var selErr))
                {
                    issues.Add(new FsTemplateIssue(r.Code, "Selector", FsIssueSeverity.Error, selErr!));
                }

                if (r.ValueType is null)
                {
                    issues.Add(new FsTemplateIssue(r.Code, "ValueType", FsIssueSeverity.Error, "نوع مقدار (مانده پایان، گردش، …) تعیین نشده است."));
                }

                if (r.NormalBalance is null)
                {
                    issues.Add(new FsTemplateIssue(r.Code, "NormalBalance", FsIssueSeverity.Error, "ماهیت ردیف (بدهکار/بستانکار) تعیین نشده است."));
                }

                break;

            case FsRowType.Formula:
                if (string.IsNullOrWhiteSpace(r.Formula))
                {
                    issues.Add(new FsTemplateIssue(r.Code, "Formula", FsIssueSeverity.Error, "ردیف فرمول به فرمول نیاز دارد."));
                }
                else if (!FsFormula.TryParse(r.Formula, out var f, out var fErr))
                {
                    issues.Add(new FsTemplateIssue(r.Code, "Formula", FsIssueSeverity.Error, fErr!));
                }
                else
                {
                    formulas[r.Code] = f!;
                }

                if (r.NormalBalance is null)
                {
                    issues.Add(new FsTemplateIssue(r.Code, "NormalBalance", FsIssueSeverity.Error, "ماهیت ردیف (بدهکار/بستانکار) تعیین نشده است."));
                }

                break;

            case FsRowType.External:
                if (r.NormalBalance is null)
                {
                    issues.Add(new FsTemplateIssue(r.Code, "NormalBalance", FsIssueSeverity.Error, "ماهیت ردیف (بدهکار/بستانکار) تعیین نشده است."));
                }

                break;
        }

        if (r.RowType != FsRowType.Account && !string.IsNullOrWhiteSpace(r.Selector))
        {
            issues.Add(new FsTemplateIssue(r.Code, "Selector", FsIssueSeverity.Warning, "فقط ردیف حساب انتخاب‌گر دارد؛ این مقدار نادیده گرفته می‌شود."));
        }

        if (r.RowType != FsRowType.Formula && !string.IsNullOrWhiteSpace(r.Formula))
        {
            issues.Add(new FsTemplateIssue(r.Code, "Formula", FsIssueSeverity.Warning, "فقط ردیف فرمول، فرمول دارد؛ این مقدار نادیده گرفته می‌شود."));
        }
    }

    /// <summary>
    /// ارجاع دوری در دورهٔ جاری. <c>PRIOR</c> و ارجاع با کلید ستون (<c>A01.PRV</c>) به ستون دیگری
    /// اشاره دارند و در گراف نمی‌آیند. <c>SUM(a:b)</c> به همهٔ ردیف‌های مقداری بازه وابسته است.
    /// </summary>
    private static void CheckCycles(
        List<FsCheckRow> ordered,
        Dictionary<string, FsFormula> formulas,
        Dictionary<string, int> index,
        List<FsTemplateIssue> issues)
    {
        var deps = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var (code, f) in formulas)
        {
            var list = f.CurrentPeriodRowRefs().ToList();

            foreach (var s in f.SumRanges())
            {
                for (var i = index[s.FromRowCode]; i <= index[s.ToRowCode]; i++)
                {
                    if (IsValueRow(ordered[i].RowType))
                    {
                        list.Add(ordered[i].Code);
                    }
                }
            }

            deps[code] = list;
        }

        var state = new Dictionary<string, int>(StringComparer.Ordinal); // 1 = در پشته، 2 = تمام
        var stack = new List<string>();

        foreach (var code in deps.Keys)
        {
            if (Visit(code))
            {
                return;
            }
        }

        bool Visit(string code)
        {
            if (state.TryGetValue(code, out var s))
            {
                if (s == 1)
                {
                    var cycle = stack.Skip(stack.IndexOf(code)).Append(code);
                    issues.Add(new FsTemplateIssue(code, "Formula", FsIssueSeverity.Error,
                        "ارجاع دوری: " + string.Join(" ← ", cycle)));
                    return true;
                }

                return false;
            }

            state[code] = 1;
            stack.Add(code);

            if (deps.TryGetValue(code, out var children))
            {
                foreach (var c in children)
                {
                    if (Visit(c))
                    {
                        return true;
                    }
                }
            }

            stack.RemoveAt(stack.Count - 1);
            state[code] = 2;
            return false;
        }
    }
}
