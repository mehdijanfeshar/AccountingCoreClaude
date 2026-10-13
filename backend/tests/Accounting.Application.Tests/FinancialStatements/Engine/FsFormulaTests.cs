using Accounting.Application.FinancialStatements.Engine;

namespace Accounting.Application.Tests.FinancialStatements.Engine;

/// <summary>تجزیهٔ فرمول ردیف قالب (دستور زبان در توضیح FsFormula).</summary>
public sealed class FsFormulaTests
{
    [Fact]
    public void Parse_Addition_And_Multiplication_RespectPrecedence()
    {
        var root = FsFormula.Parse("A01 + A02 * 2").Root;

        var add = Assert.IsType<FsBinaryExpr>(root);
        Assert.Equal('+', add.Op);
        Assert.Equal(new FsRowRefExpr("A01", null), add.Left);
        var mul = Assert.IsType<FsBinaryExpr>(add.Right);
        Assert.Equal('*', mul.Op);
        Assert.Equal(new FsNumberExpr(2m), mul.Right);
    }

    [Fact]
    public void Parse_Parentheses_OverridePrecedence()
    {
        var root = Assert.IsType<FsBinaryExpr>(FsFormula.Parse("(A01 + A02) * A03").Root);

        Assert.Equal('*', root.Op);
        Assert.IsType<FsBinaryExpr>(root.Left);
    }

    [Fact]
    public void Parse_SubtractionIsLeftAssociative()
    {
        var root = Assert.IsType<FsBinaryExpr>(FsFormula.Parse("A01 - A02 - A03").Root);

        Assert.Equal('-', root.Op);
        Assert.Equal(new FsRowRefExpr("A03", null), root.Right);
        Assert.IsType<FsBinaryExpr>(root.Left);
    }

    [Fact]
    public void Parse_UnaryMinus_BuildsUnaryNode_And_UnaryPlusIsDropped()
    {
        var neg = Assert.IsType<FsUnaryExpr>(FsFormula.Parse("-A01").Root);
        Assert.Equal('-', neg.Op);

        Assert.Equal(new FsRowRefExpr("A01", null), FsFormula.Parse("+A01").Root);
    }

    [Fact]
    public void Parse_PersianAndArabicDigits_AreNormalized()
    {
        Assert.Equal(new FsNumberExpr(1250m), FsFormula.Parse("۱۲۵۰").Root);
        Assert.Equal(new FsNumberExpr(7m), FsFormula.Parse("٧").Root);
    }

    [Fact]
    public void Parse_DecimalNumber()
    {
        Assert.Equal(new FsNumberExpr(1.5m), FsFormula.Parse("1.5").Root);
    }

    [Fact]
    public void Parse_RowRef_WithColumnKey()
    {
        Assert.Equal(new FsRowRefExpr("A01", "PRV"), FsFormula.Parse("A01.PRV").Root);
    }

    [Fact]
    public void Parse_SumRange_AndFunctionNamesAreCaseInsensitive()
    {
        var sum = Assert.IsType<FsSumRangeExpr>(FsFormula.Parse("sum(A01:A08)").Root);

        Assert.Equal("A01", sum.FromRowCode);
        Assert.Equal("A08", sum.ToRowCode);
    }

    [Fact]
    public void Parse_Prior_And_Stmt_WithDottedTemplateCode()
    {
        Assert.Equal(new FsPriorExpr("A01"), FsFormula.Parse("PRIOR(A01)").Root);
        Assert.Equal(new FsStatementRefExpr("PENSION.CHANGES", "X99"), FsFormula.Parse("STMT(PENSION.CHANGES, X99)").Root);
    }

    [Fact]
    public void Parse_Round_And_Abs()
    {
        var round = Assert.IsType<FsFunctionExpr>(FsFormula.Parse("ROUND(A01 / 3, 2)").Root);
        Assert.Equal("ROUND", round.Name);
        Assert.Equal(new FsNumberExpr(2m), round.Args[1]);

        var abs = Assert.IsType<FsFunctionExpr>(FsFormula.Parse("ABS(A01 - A02)").Root);
        Assert.Equal("ABS", abs.Name);
        Assert.Single(abs.Args);
    }

    [Fact]
    public void Parse_Round_AcceptsPersianCommaSeparator()
    {
        var round = Assert.IsType<FsFunctionExpr>(FsFormula.Parse("ROUND(A01،2)").Root);
        Assert.Equal(2, round.Args.Count);
    }

    [Theory]
    [InlineData("=")]
    [InlineData("<>")]
    [InlineData("<")]
    [InlineData("<=")]
    [InlineData(">")]
    [InlineData(">=")]
    public void Parse_If_AllComparisonOperators(string op)
    {
        var node = Assert.IsType<FsIfExpr>(FsFormula.Parse($"IF(A01 {op} A02, 1, 0)").Root);

        Assert.Equal(op, node.CompareOp);
        Assert.Equal(new FsNumberExpr(1m), node.Then);
        Assert.Equal(new FsNumberExpr(0m), node.Else);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A01 +")]
    [InlineData("A01 A02")]
    [InlineData("(A01")]
    [InlineData("A01)")]
    [InlineData("SUM")]                    // نام تابع بدون پرانتز
    [InlineData("SUM(A01)")]               // بازه بدون ":"
    [InlineData("FOO(1)")]                 // تابع ناشناخته
    [InlineData("NOTE(1)")]                // هنوز پشتیبانی نمی‌شود
    [InlineData("ROUND(A01, 13)")]         // بیش از ۱۲ رقم
    [InlineData("ROUND(A01, 2.5)")]        // رقم اعشاری
    [InlineData("ROUND(A01)")]             // آرگومان دوم نیست
    [InlineData("IF(A01, 1, 2)")]          // شرط بدون عملگر مقایسه
    [InlineData("A01 $ 2")]                // نویسهٔ نامعتبر
    [InlineData("A01.")]                   // کلید ستون ناقص
    [InlineData("STMT(PENSION.X)")]        // ردیف ندارد
    public void TryParse_InvalidFormula_ReturnsFalseWithMessage(string? text)
    {
        var ok = FsFormula.TryParse(text, out var formula, out var error);

        Assert.False(ok);
        Assert.Null(formula);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void Parse_Invalid_ThrowsFsExpressionException()
    {
        Assert.Throws<FsExpressionException>(() => FsFormula.Parse("A01 +"));
    }

    [Fact]
    public void TryParse_Valid_ReturnsFormulaAndKeepsOriginalText()
    {
        var ok = FsFormula.TryParse("A01 + ۱", out var formula, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal("A01 + ۱", formula!.Text);
    }

    [Fact]
    public void DirectRowRefs_CollectsRowPriorAndSumRefs_ButNotStmt()
    {
        var f = FsFormula.Parse("A01 + PRIOR(A02) + SUM(B01:B05) + STMT(T.X, Z99)");

        var refs = f.DirectRowRefs().ToList();

        Assert.Contains("A01", refs);
        Assert.Contains("A02", refs);
        Assert.Contains("B01", refs);
        Assert.Contains("B05", refs);
        Assert.DoesNotContain("Z99", refs);
    }

    [Fact]
    public void CurrentPeriodRowRefs_ExcludesPriorAndColumnKeyRefs()
    {
        var f = FsFormula.Parse("A01 + PRIOR(A02) + A03.PRV + IF(A04 > 0, A05, 0)");

        var refs = f.CurrentPeriodRowRefs().ToList();

        Assert.Equal(new[] { "A01", "A04", "A05" }, refs.OrderBy(x => x).ToArray());
    }

    [Fact]
    public void SumRanges_And_StatementRefs_AreFound_InsideNestedFunctions()
    {
        var f = FsFormula.Parse("ABS(SUM(A01:A03)) + ROUND(STMT(P.Q, R1), 0)");

        Assert.Single(f.SumRanges());
        Assert.Equal("P.Q", Assert.Single(f.StatementRefs()).TemplateCode);
    }
}
