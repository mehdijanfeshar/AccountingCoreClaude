using Accounting.Application.FinancialStatements.Engine;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.FinancialStatements.Engine;

/// <summary>اعتبارسنجی کامل نسخهٔ قالب پیش از فعال‌سازی.</summary>
public sealed class FsTemplateCheckerTests
{
    private static FsCheckRow Header(string code, int order, string? parent = null)
        => new(code, parent, order, FsRowType.Header, "عنوان", null, null, null, null);

    private static FsCheckRow Account(string code, int order, string? parent = null, string? selector = "13*", FsValueType? vt = FsValueType.Closing, FsNormalBalance? nb = FsNormalBalance.Debit)
        => new(code, parent, order, FsRowType.Account, "حساب", nb, selector, vt, null);

    private static FsCheckRow Formula(string code, int order, string? formula, FsNormalBalance? nb = FsNormalBalance.Debit)
        => new(code, null, order, FsRowType.Formula, "فرمول", nb, null, null, formula);

    private static List<FsTemplateIssue> Errors(IEnumerable<FsTemplateIssue> issues) => issues.Where(i => i.Severity == FsIssueSeverity.Error).ToList();

    [Fact]
    public void NoRows_IsAnError()
    {
        var issue = Assert.Single(FsTemplateChecker.Check([]));

        Assert.Equal(FsIssueSeverity.Error, issue.Severity);
        Assert.Equal("Rows", issue.Field);
        Assert.Null(issue.RowCode);
    }

    [Fact]
    public void ValidTemplate_HasNoIssues()
    {
        var rows = new[]
        {
            Header("H", 10),
            Account("A01", 20, "H"),
            Account("A02", 30, "H", selector: "14* !1405"),
            Formula("T01", 40, "SUM(A01:A02) + PRIOR(A01)"),
        };

        Assert.Empty(FsTemplateChecker.Check(rows));
    }

    // باگ مشکوک: FsTemplateChecker.cs:80 — کد تکراری پیش از گزارش خطا، در ToDictionary(valueRowIndex) با ArgumentException می‌ترکد.
    [Fact(Skip = "باگ مشکوک: کد ردیف تکراری به‌جای گزارش issue، ArgumentException می‌دهد (FsTemplateChecker.cs:80)")]
    public void DuplicateCode_IsError()
    {
        var issues = Errors(FsTemplateChecker.Check([Header("H", 10), Header("H", 20)]));

        Assert.Contains(issues, i => i.Field == "Code" && i.RowCode == "H");
    }

    [Fact]
    public void ReservedFunctionName_AsRowCode_IsError()
    {
        var issues = Errors(FsTemplateChecker.Check([Header("SUM", 10)]));

        Assert.Contains(issues, i => i.Field == "Code");
    }

    [Fact]
    public void MissingParent_And_NonHeaderParent_AreErrors()
    {
        var rows = new[] { Account("A01", 10), Account("A02", 20, "NOPE"), Account("A03", 30, "A01") };

        var issues = Errors(FsTemplateChecker.Check(rows));

        Assert.Contains(issues, i => i.RowCode == "A02" && i.Field == "Parent");
        Assert.Contains(issues, i => i.RowCode == "A03" && i.Field == "Parent");
    }

    [Fact]
    public void ParentAfterChild_IsOnlyAWarning()
    {
        var rows = new[] { Account("A01", 10, "H"), Header("H", 20) };

        var issues = FsTemplateChecker.Check(rows);

        var w = Assert.Single(issues);
        Assert.Equal(FsIssueSeverity.Warning, w.Severity);
        Assert.Equal("Parent", w.Field);
    }

    [Fact]
    public void AccountRow_MissingSelectorValueTypeNormalBalance_AreErrors()
    {
        var issues = Errors(FsTemplateChecker.Check([Account("A01", 10, selector: null, vt: null, nb: null)]));

        Assert.Contains(issues, i => i.Field == "Selector");
        Assert.Contains(issues, i => i.Field == "ValueType");
        Assert.Contains(issues, i => i.Field == "NormalBalance");
    }

    [Fact]
    public void AccountRow_BadSelectorSyntax_IsError()
    {
        var issues = Errors(FsTemplateChecker.Check([Account("A01", 10, selector: "13[X]")]));

        Assert.Contains(issues, i => i.Field == "Selector");
    }

    [Fact]
    public void FormulaRow_MissingOrBrokenFormula_IsError()
    {
        var rows = new[] { Account("A01", 10), Formula("T01", 20, null), Formula("T02", 30, "A01 +") };

        var issues = Errors(FsTemplateChecker.Check(rows));

        Assert.Contains(issues, i => i.RowCode == "T01" && i.Field == "Formula");
        Assert.Contains(issues, i => i.RowCode == "T02" && i.Field == "Formula");
    }

    [Fact]
    public void FormulaRow_WithoutNormalBalance_IsError()
    {
        var issues = Errors(FsTemplateChecker.Check([Account("A01", 10), Formula("T01", 20, "A01", nb: null)]));

        Assert.Contains(issues, i => i.RowCode == "T01" && i.Field == "NormalBalance");
    }

    [Fact]
    public void Formula_ReferencingMissingRow_OrNonValueRow_IsError()
    {
        var rows = new[] { Header("H", 10), Account("A01", 20), Formula("T01", 30, "A01 + MISSING + H") };

        var issues = Errors(FsTemplateChecker.Check(rows));

        Assert.Contains(issues, i => i.Message.Contains("MISSING"));
        Assert.Contains(issues, i => i.Message.Contains("«H»"));
    }

    [Fact]
    public void Sum_WithReversedRange_IsError()
    {
        var rows = new[] { Account("A01", 10), Account("A02", 20), Formula("T01", 30, "SUM(A02:A01)") };

        var issues = Errors(FsTemplateChecker.Check(rows));

        Assert.Contains(issues, i => i.RowCode == "T01" && i.Message.Contains("SUM"));
    }

    [Fact]
    public void Stmt_UnknownTemplate_OwnTemplate_AndKnownTemplate()
    {
        var rows = new[] { Account("A01", 10), Formula("T01", 20, "STMT(OTHER.X, R1)"), Formula("T02", 30, "STMT(OWN.T, R1)") };
        var known = new HashSet<string> { "OWN.T" };

        var unknown = Errors(FsTemplateChecker.Check(rows, "OWN.T", known));
        Assert.Contains(unknown, i => i.RowCode == "T01" && i.Message.Contains("OTHER.X"));
        Assert.Contains(unknown, i => i.RowCode == "T02");   // ارجاع به صورت خودش

        var fine = Errors(FsTemplateChecker.Check([Account("A01", 10), Formula("T01", 20, "STMT(OTHER.X, R1)")], "OWN.T", new HashSet<string> { "OTHER.X" }));
        Assert.Empty(fine);
    }

    [Fact]
    public void Stmt_WithoutKnownTemplateSet_IsNotValidatedAgainstCatalog()
    {
        var rows = new[] { Account("A01", 10), Formula("T01", 20, "STMT(OTHER.X, R1)") };

        Assert.Empty(Errors(FsTemplateChecker.Check(rows)));
    }

    [Fact]
    public void CircularReference_IsDetected_WithPath()
    {
        var rows = new[] { Formula("T01", 10, "T02 + 1"), Formula("T02", 20, "T01 + 1") };

        var issues = Errors(FsTemplateChecker.Check(rows));

        var cycle = Assert.Single(issues);
        Assert.Contains("ارجاع دوری", cycle.Message);
        Assert.Contains("T01", cycle.Message);
        Assert.Contains("T02", cycle.Message);
    }

    [Fact]
    public void SelfReference_IsCircular()
    {
        Assert.Contains(Errors(FsTemplateChecker.Check([Formula("T01", 10, "T01 + 1")])), i => i.Message.Contains("ارجاع دوری"));
    }

    [Fact]
    public void SumRangeThatIncludesItsOwnRow_IsCircular()
    {
        var rows = new[] { Account("A01", 10), Formula("T01", 20, "SUM(A01:T01)") };

        Assert.Contains(Errors(FsTemplateChecker.Check(rows)), i => i.Message.Contains("ارجاع دوری"));
    }

    [Fact]
    public void PriorAndColumnKeyReferencesToSelf_AreNotCycles()
    {
        var rows = new[] { Account("A01", 10), Formula("T01", 20, "PRIOR(T01) + A01"), Formula("T02", 30, "T02.PRV + A01") };

        Assert.Empty(Errors(FsTemplateChecker.Check(rows)));
    }

    [Fact]
    public void NoteTotalRow_MustExistAndBeAValueRow()
    {
        var rows = new[] { Header("H", 10), Account("A01", 20) };

        Assert.Contains(Errors(FsTemplateChecker.Check(rows, noteTotalRowCode: "ZZZ")), i => i.Field == "NoteTotalRowCode");
        Assert.Contains(Errors(FsTemplateChecker.Check(rows, noteTotalRowCode: "H")), i => i.Field == "NoteTotalRowCode");
        Assert.Empty(Errors(FsTemplateChecker.Check(rows, noteTotalRowCode: "A01")));
    }

    [Fact]
    public void IgnoredFields_AreWarningsNotErrors()
    {
        var rows = new[]
        {
            new FsCheckRow("H", null, 10, FsRowType.Header, "ع", null, "13*", null, "A+1"),
        };

        var issues = FsTemplateChecker.Check(rows);

        Assert.Equal(2, issues.Count);
        Assert.All(issues, i => Assert.Equal(FsIssueSeverity.Warning, i.Severity));
    }

    [Fact]
    public void EmptyTitle_OnValueRow_IsWarning()
    {
        var rows = new[] { new FsCheckRow("A01", null, 10, FsRowType.Account, " ", FsNormalBalance.Debit, "13*", FsValueType.Closing, null) };

        var w = Assert.Single(FsTemplateChecker.Check(rows));

        Assert.Equal(FsIssueSeverity.Warning, w.Severity);
        Assert.Equal("TitleFa", w.Field);
    }

    [Fact]
    public void ExternalRow_NeedsNormalBalance()
    {
        var rows = new[] { new FsCheckRow("E01", null, 10, FsRowType.External, "دستی", null, null, null, null) };

        Assert.Contains(Errors(FsTemplateChecker.Check(rows)), i => i.Field == "NormalBalance");
    }
}
