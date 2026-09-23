using Accounting.Application.Reports.CrossTab;
using Accounting.Application.Reports.CrossTab.GetCrossTabReport;

namespace Accounting.Application.Tests.Reports.CrossTab;

public sealed class GetCrossTabReportQueryValidatorTests
{
    private static readonly GetCrossTabReportQueryValidator Validator = new();

    private static GetCrossTabReportQuery ValidQuery() => new(
        Year: "1403",
        RowDimension: CrossTabDimension.Tafsili1,
        ColumnDimension: CrossTabDimension.Moin,
        FromDate: null,
        ToDate: null,
        DocLife: null,
        SystemTypeId: null,
        RowCodeFilter: null,
        ColumnCodeFilter: null);

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        Assert.True(Validator.Validate(ValidQuery()).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("14")]
    [InlineData("14035")]
    [InlineData("abcd")]
    public void Validate_BadYear_Fails(string year)
    {
        Assert.False(Validator.Validate(ValidQuery() with { Year = year }).IsValid);
    }

    /// <summary>
    /// The load-bearing rule. Crossing a dimension with itself leaves every off-diagonal cell
    /// empty by construction, and answering it with a mostly-blank grid is indistinguishable from
    /// «داده‌ای نیست».
    /// </summary>
    [Fact]
    public void Validate_SameDimensionOnBothAxes_Fails()
    {
        var result = Validator.Validate(ValidQuery() with
        {
            RowDimension = CrossTabDimension.Moin,
            ColumnDimension = CrossTabDimension.Moin,
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("یکی"));
    }

    [Fact]
    public void Validate_EveryDistinctDimensionPair_Passes()
    {
        var all = Enum.GetValues<CrossTabDimension>();

        foreach (var row in all)
        {
            foreach (var column in all.Where(c => c != row))
            {
                var result = Validator.Validate(ValidQuery() with
                {
                    RowDimension = row,
                    ColumnDimension = column,
                });
                Assert.True(result.IsValid, $"{row} × {column} should be valid");
            }
        }
    }

    [Fact]
    public void Validate_DimensionOutsideEnum_Fails()
    {
        Assert.False(Validator.Validate(ValidQuery() with { RowDimension = (CrossTabDimension)99 }).IsValid);
        Assert.False(Validator.Validate(ValidQuery() with { ColumnDimension = (CrossTabDimension)0 }).IsValid);
    }

    [Theory]
    [InlineData("1403051")]
    [InlineData("1403/05/15")]
    public void Validate_MalformedDate_Fails(string date)
    {
        Assert.False(Validator.Validate(ValidQuery() with { FromDate = date }).IsValid);
        Assert.False(Validator.Validate(ValidQuery() with { ToDate = date }).IsValid);
    }

    /// <summary>
    /// An inverted range silently returns nothing, which reads to the user as missing data rather
    /// than as bounds typed backwards.
    /// </summary>
    [Fact]
    public void Validate_InvertedDateRange_Fails()
    {
        Assert.False(Validator.Validate(ValidQuery() with
        {
            FromDate = "14031229",
            ToDate = "14030101",
        }).IsValid);
    }

    [Fact]
    public void Validate_EqualDateBounds_Pass()
    {
        Assert.True(Validator.Validate(ValidQuery() with
        {
            FromDate = "14030515",
            ToDate = "14030515",
        }).IsValid);
    }

    [Fact]
    public void Validate_OverlongCodeFilter_Fails()
    {
        var tooLong = new string('1', GetCrossTabReportQueryValidator.MaxCodeLength + 1);

        Assert.False(Validator.Validate(ValidQuery() with { RowCodeFilter = tooLong }).IsValid);
        Assert.False(Validator.Validate(ValidQuery() with { ColumnCodeFilter = tooLong }).IsValid);
    }

    [Fact]
    public void Validate_EmptySystemTypeId_Fails()
    {
        Assert.False(Validator.Validate(ValidQuery() with { SystemTypeId = Guid.Empty }).IsValid);
    }
}
