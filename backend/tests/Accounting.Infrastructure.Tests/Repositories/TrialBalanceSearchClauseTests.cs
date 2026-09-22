using Accounting.Application.Common.Search;
using Accounting.Application.Reports.TrialBalance;
using Accounting.Infrastructure.Repositories;

namespace Accounting.Infrastructure.Tests.Repositories;

/// <summary>
/// Tests the only place in this codebase where caller-supplied filtering becomes SQL text.
///
/// <para>
/// The Oracle statement itself cannot be executed here — it uses <c>NVL</c>, <c>ESCAPE</c> and the
/// Jalali string comparison, none of which SQLite speaks, and the project's standing rule forbids
/// touching live Oracle. So these assert the thing that actually matters and is checkable without a
/// database: <b>nothing the caller typed appears in the generated SQL.</b> Field names come from a
/// map the repository owns, operators from a closed enum, values from binds.
/// </para>
/// </summary>
public sealed class TrialBalanceSearchClauseTests
{
    private static readonly IReadOnlyDictionary<string, string> FieldMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [TrialBalanceSearchFields.Code] = "a.ACCCODE",
            [TrialBalanceSearchFields.Description] = "a.ACCCODENAME",
        };

    private static SearchParam Filter(string property, SearchOperator op, string value) =>
        new() { Property = property, Operator = op, Value = value };

    private static (string Sql, IReadOnlyList<Oracle.ManagedDataAccess.Client.OracleParameter> Parameters) Build(
        params SearchParam[] filters) =>
        TrialBalanceReadRepository.BuildSearchClauses(filters, FieldMap);

    [Fact]
    public void NoFilters_ProduceNoSqlAndNoParameters()
    {
        var (sql, parameters) = TrialBalanceReadRepository.BuildSearchClauses(null, FieldMap);

        Assert.Equal(string.Empty, sql);
        Assert.Empty(parameters);
    }

    [Fact]
    public void TheValueNeverAppearsInTheSql_OnlyABind()
    {
        // The single most important assertion in this file. If a value ever reached the statement
        // text, every other protection here would be decoration.
        var (sql, parameters) = Build(Filter("code", SearchOperator.EQ, "110401"));

        Assert.DoesNotContain("110401", sql, StringComparison.Ordinal);
        Assert.Contains("a.ACCCODE = :f0", sql, StringComparison.Ordinal);
        Assert.Equal("110401", Assert.Single(parameters).Value);
    }

    [Fact]
    public void LikeWrapsTheTermAndBindsIt()
    {
        var (sql, parameters) = Build(Filter("description", SearchOperator.LIKE, "بانک"));

        Assert.Contains("UPPER(a.ACCCODENAME) LIKE '%' || UPPER(:f0) || '%'", sql, StringComparison.Ordinal);
        Assert.Contains("ESCAPE", sql, StringComparison.Ordinal);
        Assert.Equal("بانک", Assert.Single(parameters).Value);
    }

    [Theory]
    [InlineData("%", "\\%")]
    [InlineData("1_0", "1\\_0")]
    [InlineData("a\\b", "a\\\\b")]
    public void LikeWildcardsInTheTerm_AreEscapedSoTheyStayLiteral(string input, string expected)
    {
        // Not an injection concern — the term is bound either way. This is about meaning: an
        // unescaped "%" would quietly turn a search into "every account in the unit".
        var (_, parameters) = Build(Filter("code", SearchOperator.LIKE, input));

        Assert.Equal(expected, Assert.Single(parameters).Value);
    }

    [Fact]
    public void EqualityOperatorsDoNotEscape_BecauseTheyAreNotPatterns()
    {
        var (_, parameters) = Build(Filter("code", SearchOperator.EQ, "10%"));

        Assert.Equal("10%", Assert.Single(parameters).Value);
    }

    [Theory]
    [InlineData(SearchOperator.EQ, "=")]
    [InlineData(SearchOperator.NEQ, "<>")]
    [InlineData(SearchOperator.GT, ">")]
    [InlineData(SearchOperator.LT, "<")]
    [InlineData(SearchOperator.GTE, ">=")]
    [InlineData(SearchOperator.LTE, "<=")]
    public void EachOperator_MapsToItsSqlToken(SearchOperator op, string expected)
    {
        var (sql, _) = Build(Filter("code", op, "1"));

        Assert.Contains($"a.ACCCODE {expected} :f0", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void In_BindsEachItemSeparately()
    {
        // Pasting the list in as one comma-separated string would put caller text into the
        // statement; each item gets its own bind instead.
        var (sql, parameters) = Build(Filter("code", SearchOperator.IN, "11, 12 ,13"));

        Assert.Contains("a.ACCCODE IN (:f0_0, :f0_1, :f0_2)", sql, StringComparison.Ordinal);
        Assert.Equal(3, parameters.Count);
        Assert.Equal(new[] { "11", "12", "13" }, parameters.Select(p => (string)p.Value!));
    }

    [Fact]
    public void In_WithNoItems_Throws_RatherThanSilentlyWideningTheReport()
    {
        Assert.Throws<ArgumentException>(() => Build(Filter("code", SearchOperator.IN, " , ")));
    }

    [Fact]
    public void UnmappedField_Throws_RatherThanBeingSkipped()
    {
        // The validator rejects unknown names first, so reaching here means our two lists drifted.
        // Dropping the clause would return a wider report than asked for while looking correct.
        Assert.Throws<ArgumentException>(() => Build(Filter("VAHEDCODE", SearchOperator.EQ, "0043")));
    }

    [Fact]
    public void MultipleFilters_GetDistinctBindNames()
    {
        var (sql, parameters) = Build(
            Filter("code", SearchOperator.GTE, "11"),
            Filter("code", SearchOperator.LTE, "19"));

        Assert.Contains(":f0", sql, StringComparison.Ordinal);
        Assert.Contains(":f1", sql, StringComparison.Ordinal);
        Assert.Equal(2, parameters.Count);
    }

    [Fact]
    public void EveryAllowedField_IsMapped()
    {
        // Guards the drift the repository's throw only detects at runtime: a name added to
        // TrialBalanceSearchFields without a SQL expression would pass validation and then 500.
        foreach (var field in TrialBalanceSearchFields.All)
        {
            Assert.True(FieldMap.ContainsKey(field), $"'{field}' is allowed but has no SQL mapping.");
        }
    }
}
