using Accounting.Application.Reports.MatrixReport;
using Accounting.Application.Reports.MatrixReport.GetMatrixReport;

namespace Accounting.Application.Tests.Reports.MatrixReport;

/// <summary>
/// The scope rules are the ones worth pinning. A contradictory path — a level repeated, or a step
/// deeper than the level being listed — would otherwise answer with an empty table, which is
/// indistinguishable from «داده‌ای نیست» and sends the user looking for missing data instead of a
/// bad request.
/// </summary>
public sealed class GetMatrixReportQueryValidatorTests
{
    private readonly GetMatrixReportQueryValidator _validator = new();

    private static MatrixReportScopeItem Step(MatrixReportLevel level, string code)
        => new() { Level = level, Code = code };

    private static GetMatrixReportQuery ValidQuery(params MatrixReportScopeItem[] scope) => new(
        Year: "1404",
        Level: MatrixReportLevel.Moin,
        Scope: scope,
        FromDate: "14040101",
        ToDate: "14041230",
        FromVoucherNo: "1",
        ToVoucherNo: "100",
        DocLife: 2,
        SystemTypeId: null)
    {
        VahedCode = "0001",
    };

    [Fact]
    public void Valid_query_without_a_scope_passes()
    {
        Assert.True(_validator.Validate(ValidQuery()).IsValid);
    }

    [Fact]
    public void A_scope_of_shallower_levels_passes()
    {
        var query = ValidQuery(
            Step(MatrixReportLevel.Group, "1"),
            Step(MatrixReportLevel.Kol, "10"));

        Assert.True(_validator.Validate(query).IsValid);
    }

    [Fact]
    public void A_scope_step_at_or_below_the_listed_level_is_rejected()
    {
        Assert.False(_validator.Validate(ValidQuery(Step(MatrixReportLevel.Moin, "1010"))).IsValid);
        Assert.False(_validator.Validate(ValidQuery(Step(MatrixReportLevel.Tafsili1, "T1"))).IsValid);
    }

    [Fact]
    public void The_same_level_twice_is_rejected()
    {
        var query = ValidQuery(
            Step(MatrixReportLevel.Group, "1"),
            Step(MatrixReportLevel.Group, "2"));

        Assert.False(_validator.Validate(query).IsValid);
    }

    [Fact]
    public void A_scope_step_needs_a_code()
    {
        Assert.False(_validator.Validate(ValidQuery(Step(MatrixReportLevel.Group, ""))).IsValid);
    }

    [Fact]
    public void A_code_longer_than_the_widest_column_is_rejected()
    {
        var tooLong = new string('9', GetMatrixReportQueryValidator.MaxCodeLength + 1);

        Assert.False(_validator.Validate(ValidQuery(Step(MatrixReportLevel.Group, tooLong))).IsValid);
    }

    [Fact]
    public void A_scope_step_at_an_unknown_level_is_rejected()
    {
        Assert.False(_validator.Validate(ValidQuery(Step((MatrixReportLevel)99, "1"))).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("140")]
    [InlineData("سال")]
    public void Year_must_be_four_digits(string year)
    {
        Assert.False(_validator.Validate(ValidQuery() with { Year = year }).IsValid);
    }

    [Fact]
    public void Inverted_date_range_is_rejected()
    {
        Assert.False(_validator
            .Validate(ValidQuery() with { FromDate = "14041230", ToDate = "14040101" })
            .IsValid);
    }

    [Fact]
    public void Doc_life_outside_the_known_range_is_rejected()
    {
        Assert.False(_validator.Validate(ValidQuery() with { DocLife = 9 }).IsValid);
    }
}
