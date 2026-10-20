using Accounting.Application.Reports.GeneralLedger;

namespace Accounting.Application.Tests.Reports.GeneralLedger;

/// <summary>
/// فاز ۴۹-الف — ورودی دفتر کل. خودِ کوئری SQL خام اوراکل است و روی SQLite قابل تست نیست (ریسک #۲۵).
/// </summary>
public class GetGeneralLedgerQueryValidatorTests
{
    private readonly GetGeneralLedgerQueryValidator _v = new();

    [Fact]
    public void Minimal_IsValid() => Assert.True(_v.Validate(new GetGeneralLedgerQuery("1405")).IsValid);

    [Fact]
    public void FullFilter_IsValid() =>
        Assert.True(_v.Validate(new GetGeneralLedgerQuery("1405", "14050101", "14051229", "11", "9999", 2, "1", "999999", 3, 1000)).IsValid);

    [Theory]
    [InlineData("140")]
    [InlineData("14O5")]
    [InlineData("")]
    public void Year_MustBeFourDigits(string year) => Assert.False(_v.Validate(new GetGeneralLedgerQuery(year)).IsValid);

    [Theory]
    [InlineData("1405/01/01", null)]
    [InlineData(null, "140512")]
    public void Dates_MustBeYyyyMmDd(string? from, string? to) =>
        Assert.False(_v.Validate(new GetGeneralLedgerQuery("1405", from, to)).IsValid);

    [Fact]
    public void FromDate_AfterToDate_IsInvalid() =>
        Assert.False(_v.Validate(new GetGeneralLedgerQuery("1405", "14050201", "14050101")).IsValid);

    [Theory]
    [InlineData("12345", null)]
    [InlineData(null, "ab")]
    public void KolCode_UpToFourDigits(string? from, string? to) =>
        Assert.False(_v.Validate(new GetGeneralLedgerQuery("1405", FromKol: from, ToKol: to)).IsValid);

    [Theory]
    [InlineData("1234567", null)]
    [InlineData(null, "12a")]
    public void VoucherNo_UpToSixDigits(string? from, string? to) =>
        Assert.False(_v.Validate(new GetGeneralLedgerQuery("1405", FromVoucherNo: from, ToVoucherNo: to)).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void DocLife_Between1And4(int life) =>
        Assert.False(_v.Validate(new GetGeneralLedgerQuery("1405", DocLife: life)).IsValid);

    [Theory]
    [InlineData(0, 100)]
    [InlineData(1, 0)]
    [InlineData(1, 1001)]
    public void Paging_Bounds(int page, int size) =>
        Assert.False(_v.Validate(new GetGeneralLedgerQuery("1405", PageNumber: page, PageSize: size)).IsValid);

    [Fact]
    public void BlankOptionalFilters_AreIgnored() =>
        Assert.True(_v.Validate(new GetGeneralLedgerQuery("1405", " ", " ", " ", " ", null, " ", " ")).IsValid);
}
