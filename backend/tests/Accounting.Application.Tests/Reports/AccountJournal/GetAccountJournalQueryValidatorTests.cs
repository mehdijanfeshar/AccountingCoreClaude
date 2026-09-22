using Accounting.Application.Reports.AccountJournal.GetAccountJournal;

namespace Accounting.Application.Tests.Reports.AccountJournal;

public sealed class GetAccountJournalQueryValidatorTests
{
    private readonly GetAccountJournalQueryValidator _validator = new();

    private static GetAccountJournalQuery ValidQuery() => new(
        PageNumber: 1,
        PageSize: 50,
        Year: "1404",
        FromVoucherNo: "1",
        ToVoucherNo: "100",
        FromDate: "14040101",
        ToDate: "14041230",
        FromAccountCode: "110001",
        ToAccountCode: "220001",
        DocLife: 2,
        Description: "حقوق")
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
        var query = ValidQuery() with
        {
            FromVoucherNo = null,
            ToVoucherNo = null,
            FromDate = null,
            ToDate = null,
            FromAccountCode = null,
            ToAccountCode = null,
            DocLife = null,
            Description = null,
        };

        Assert.True(_validator.Validate(query).IsValid);
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
    public void Page_size_is_capped_higher_than_the_review_report()
    {
        // A journal is read in long runs, which is why its cap is larger — but it is still a cap.
        Assert.True(_validator
            .Validate(ValidQuery() with { PageSize = GetAccountJournalQueryValidator.MaxPageSize })
            .IsValid);
        Assert.False(_validator
            .Validate(ValidQuery() with { PageSize = GetAccountJournalQueryValidator.MaxPageSize + 1 })
            .IsValid);
    }

    [Fact]
    public void Inverted_date_range_is_rejected()
    {
        Assert.False(_validator
            .Validate(ValidQuery() with { FromDate = "14041230", ToDate = "14040101" })
            .IsValid);
    }

    /// <summary>
    /// Account codes are compared ordinally and that is correct — unlike voucher numbers they are
    /// not zero-padded numbers but codes, and «۱۱» really does come before «۲۲».
    /// </summary>
    [Fact]
    public void Inverted_account_code_range_is_rejected()
    {
        Assert.True(_validator
            .Validate(ValidQuery() with { FromAccountCode = "11", ToAccountCode = "22" })
            .IsValid);
        Assert.False(_validator
            .Validate(ValidQuery() with { FromAccountCode = "22", ToAccountCode = "11" })
            .IsValid);
    }

    [Fact]
    public void Account_code_longer_than_the_column_is_rejected()
    {
        Assert.False(_validator.Validate(ValidQuery() with { FromAccountCode = "1100011" }).IsValid);
    }

    [Fact]
    public void Voucher_number_range_is_compared_numerically_not_ordinally()
    {
        Assert.True(_validator.Validate(ValidQuery() with { FromVoucherNo = "9", ToVoucherNo = "20" }).IsValid);
        Assert.False(_validator.Validate(ValidQuery() with { FromVoucherNo = "20", ToVoucherNo = "9" }).IsValid);
    }

    [Fact]
    public void Doc_life_outside_the_known_range_is_rejected()
    {
        Assert.False(_validator.Validate(ValidQuery() with { DocLife = 9 }).IsValid);
    }
}
