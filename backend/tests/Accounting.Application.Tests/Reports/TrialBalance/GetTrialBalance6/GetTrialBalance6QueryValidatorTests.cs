using Accounting.Application.Reports.TrialBalance;
using Accounting.Application.Reports.TrialBalance.GetTrialBalance6;

namespace Accounting.Application.Tests.Reports.TrialBalance.GetTrialBalance6;

public sealed class GetTrialBalance6QueryValidatorTests
{
    private readonly GetTrialBalance6QueryValidator _validator = new();

    private static GetTrialBalance6Query ValidQuery() => new(
        Year: "1405",
        FromDate: "14050101",
        ToDate: "14051230",
        VahedCode: "0001",
        Level: TrialBalanceLevel.Kol,
        DocLife: 2);

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        Assert.True(_validator.Validate(ValidQuery()).IsValid);
    }

    [Fact]
    public void Validate_MissingYear_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { Year = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance6Query.Year));
    }

    [Theory]
    [InlineData("140")]
    [InlineData("14050")]
    public void Validate_WrongLengthYear_Fails(string year)
    {
        var result = _validator.Validate(ValidQuery() with { Year = year });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance6Query.Year));
    }

    [Fact]
    public void Validate_FromDateAfterToDate_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { FromDate = "14051230", ToDate = "14050101" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance6Query.FromDate));
    }

    [Theory]
    [InlineData("1405011")]
    [InlineData("140501011")]
    public void Validate_FromDateWrongLength_Fails(string fromDate)
    {
        var result = _validator.Validate(ValidQuery() with { FromDate = fromDate });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance6Query.FromDate));
    }

    [Fact]
    public void Validate_ToDateNonDigits_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { ToDate = "1405ABCD" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance6Query.ToDate));
    }

    [Fact]
    public void Validate_UndefinedLevel_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { Level = (TrialBalanceLevel)0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance6Query.Level));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void Validate_DocLifeOutOfRange_Fails(int docLife)
    {
        var result = _validator.Validate(ValidQuery() with { DocLife = docLife });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance6Query.DocLife));
    }

    [Fact]
    public void Validate_DocLifeNull_Passes()
    {
        var result = _validator.Validate(ValidQuery() with { DocLife = null });

        Assert.True(result.IsValid);
    }
}
