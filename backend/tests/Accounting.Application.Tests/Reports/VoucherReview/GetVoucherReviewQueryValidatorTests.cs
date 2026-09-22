using Accounting.Application.Reports.VoucherReview.GetVoucherReview;

namespace Accounting.Application.Tests.Reports.VoucherReview;

public sealed class GetVoucherReviewQueryValidatorTests
{
    private readonly GetVoucherReviewQueryValidator _validator = new();

    private static GetVoucherReviewQuery ValidQuery() => new(
        PageNumber: 1,
        PageSize: 20,
        Year: "1404",
        FromVoucherNo: "1",
        ToVoucherNo: "100",
        FromDate: "14040101",
        ToDate: "14041230",
        FromAtfNo: "A100",
        ToAtfNo: "A900",
        DocLife: 2,
        SystemTypeId: null,
        Description: "خرید")
    {
        VahedCode = "0001",
    };

    [Fact]
    public void Valid_query_passes()
    {
        Assert.True(_validator.Validate(ValidQuery()).IsValid);
    }

    [Fact]
    public void Every_optional_filter_may_be_null()
    {
        // VahedCode is deliberately left at its valid value: it is server-assigned by
        // VahedScopeBehavior, never caller input, so this validator carries no rule for it.
        var query = ValidQuery() with
        {
            FromVoucherNo = null,
            ToVoucherNo = null,
            FromDate = null,
            ToDate = null,
            FromAtfNo = null,
            ToAtfNo = null,
            DocLife = null,
            SystemTypeId = null,
            Description = null,
        };

        Assert.True(_validator.Validate(query).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("14")]
    [InlineData("14044")]
    [InlineData("سال")]
    public void Year_must_be_four_digits(string year)
    {
        Assert.False(_validator.Validate(ValidQuery() with { Year = year }).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_number_must_be_positive(int pageNumber)
    {
        Assert.False(_validator.Validate(ValidQuery() with { PageNumber = pageNumber }).IsValid);
    }

    [Fact]
    public void Page_size_is_capped()
    {
        Assert.False(_validator
            .Validate(ValidQuery() with { PageSize = GetVoucherReviewQueryValidator.MaxPageSize + 1 })
            .IsValid);
    }

    [Fact]
    public void Inverted_date_range_is_rejected_rather_than_returning_nothing()
    {
        var result = _validator.Validate(ValidQuery() with { FromDate = "14041230", ToDate = "14040101" });

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// The reason the voucher-number rule compares numerically instead of ordinally. The user
    /// types unpadded numbers, and ordinally "9" &gt; "20" — validating the typed form as text
    /// would reject a range the repository handles correctly once it pads the bounds.
    /// </summary>
    [Fact]
    public void Voucher_number_range_is_compared_numerically_not_ordinally()
    {
        Assert.True(_validator.Validate(ValidQuery() with { FromVoucherNo = "9", ToVoucherNo = "20" }).IsValid);
        Assert.False(_validator.Validate(ValidQuery() with { FromVoucherNo = "20", ToVoucherNo = "9" }).IsValid);
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("abc")]
    public void Voucher_number_must_be_at_most_six_digits(string voucherNo)
    {
        Assert.False(_validator.Validate(ValidQuery() with { FromVoucherNo = voucherNo }).IsValid);
    }

    [Fact]
    public void Doc_life_outside_the_known_range_is_rejected()
    {
        Assert.False(_validator.Validate(ValidQuery() with { DocLife = 9 }).IsValid);
    }
}
