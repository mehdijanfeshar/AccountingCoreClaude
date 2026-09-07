using Accounting.Application.Reports.TrialBalance;
using Accounting.Application.Reports.TrialBalance.GetTrialBalance8;

namespace Accounting.Application.Tests.Reports.TrialBalance.GetTrialBalance8;

public sealed class GetTrialBalance8QueryValidatorTests
{
    private readonly GetTrialBalance8QueryValidator _validator = new();

    private static GetTrialBalance8Query ValidQuery() => new(
        Year: "1405",
        FromDate: "14050101",
        ToDate: "14051230",
        VahedCode: "0001",
        Level: TrialBalanceLevel.Group,
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance8Query.Year));
    }

    [Theory]
    [InlineData("140")]
    [InlineData("14050")]
    public void Validate_WrongLengthYear_Fails(string year)
    {
        var result = _validator.Validate(ValidQuery() with { Year = year });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance8Query.Year));
    }

    [Fact]
    public void Validate_FromDateAfterToDate_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { FromDate = "14051230", ToDate = "14050101" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance8Query.FromDate));
    }

    [Theory]
    [InlineData("1405011")]
    [InlineData("140501011")]
    public void Validate_ToDateWrongLength_Fails(string toDate)
    {
        var result = _validator.Validate(ValidQuery() with { ToDate = toDate });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance8Query.ToDate));
    }

    [Fact]
    public void Validate_FromDateNonDigits_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { FromDate = "1405ABCD" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance8Query.FromDate));
    }

    [Fact]
    public void Validate_UndefinedLevel_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { Level = (TrialBalanceLevel)42 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance8Query.Level));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void Validate_DocLifeOutOfRange_Fails(int docLife)
    {
        var result = _validator.Validate(ValidQuery() with { DocLife = docLife });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance8Query.DocLife));
    }

    [Fact]
    public void Validate_DocLifeNull_Passes()
    {
        var result = _validator.Validate(ValidQuery() with { DocLife = null });

        Assert.True(result.IsValid);
    }
}
