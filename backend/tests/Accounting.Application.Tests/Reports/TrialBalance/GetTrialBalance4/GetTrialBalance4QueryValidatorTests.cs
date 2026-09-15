using Accounting.Application.Reports.TrialBalance;
using Accounting.Application.Reports.TrialBalance.GetTrialBalance4;

namespace Accounting.Application.Tests.Reports.TrialBalance.GetTrialBalance4;

public sealed class GetTrialBalance4QueryValidatorTests
{
    private readonly GetTrialBalance4QueryValidator _validator = new();

    private static GetTrialBalance4Query ValidQuery() => new(
        Year: "1405",
        FromDate: "14050101",
        ToDate: "14051230",
        Level: TrialBalanceLevel.Moin,
        DocLife: 2)
    {
        VahedCode = "0001",
    };

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        Assert.True(_validator.Validate(ValidQuery()).IsValid);
    }

    [Fact]
    public void Validate_AllOptionalFiltersNull_Passes()
    {
        // VahedCode is deliberately excluded here — it is no longer nullable/optional caller
        // input (server-assigned by VahedScopeBehavior via IVahedScopedQuery), so it keeps its
        // valid value from ValidQuery() rather than being set to null.
        var result = _validator.Validate(ValidQuery() with { FromDate = null, ToDate = null, DocLife = null });

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// VahedCode is deliberately not exercised by a dedicated length/emptiness rule here anymore:
    /// it is no longer optional caller input (removed from the positional parameter list entirely
    /// — see <see cref="GetTrialBalance4Query.VahedCode"/>), and this validator carries no rule
    /// for it because it is always server-assigned by <c>VahedScopeBehavior</c> before this
    /// validator ever runs.
    /// </summary>
    [Fact]
    public void Validate_ServerAssignedVahedCode_IsNotValidatedHere()
    {
        var result = _validator.Validate(ValidQuery() with { VahedCode = "0009" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MissingYear_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { Year = "" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance4Query.Year));
    }

    [Fact]
    public void Validate_NullYear_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { Year = null! });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance4Query.Year));
    }

    [Theory]
    [InlineData("140")]
    [InlineData("14050")]
    public void Validate_WrongLengthYear_Fails(string year)
    {
        var result = _validator.Validate(ValidQuery() with { Year = year });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance4Query.Year));
    }

    [Fact]
    public void Validate_FromDateAfterToDate_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { FromDate = "14051230", ToDate = "14050101" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance4Query.FromDate));
    }

    [Fact]
    public void Validate_FromDateEqualToDate_Passes()
    {
        var result = _validator.Validate(ValidQuery() with { FromDate = "14050101", ToDate = "14050101" });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("1405011")]
    [InlineData("140501011")]
    public void Validate_FromDateWrongLength_Fails(string fromDate)
    {
        var result = _validator.Validate(ValidQuery() with { FromDate = fromDate });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance4Query.FromDate));
    }

    [Fact]
    public void Validate_FromDateNonDigits_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { FromDate = "1405ABCD" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance4Query.FromDate));
    }

    [Theory]
    [InlineData("1405011")]
    [InlineData("140501011")]
    public void Validate_ToDateWrongLength_Fails(string toDate)
    {
        var result = _validator.Validate(ValidQuery() with { ToDate = toDate });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance4Query.ToDate));
    }

    [Fact]
    public void Validate_ToDateNonDigits_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { ToDate = "1405ABCD" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance4Query.ToDate));
    }

    [Fact]
    public void Validate_UndefinedLevel_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { Level = (TrialBalanceLevel)99 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance4Query.Level));
    }

    [Fact]
    public void Validate_DefaultLevel_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { Level = default });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance4Query.Level));
    }

    [Theory]
    [InlineData(TrialBalanceLevel.Group)]
    [InlineData(TrialBalanceLevel.Kol)]
    [InlineData(TrialBalanceLevel.Moin)]
    public void Validate_EveryDefinedLevel_Passes(TrialBalanceLevel level)
    {
        var result = _validator.Validate(ValidQuery() with { Level = level });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void Validate_DocLifeOutOfRange_Fails(int docLife)
    {
        var result = _validator.Validate(ValidQuery() with { DocLife = docLife });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTrialBalance4Query.DocLife));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void Validate_DocLifeAtBoundary_Passes(int docLife)
    {
        var result = _validator.Validate(ValidQuery() with { DocLife = docLife });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DocLifeNull_Passes()
    {
        var result = _validator.Validate(ValidQuery() with { DocLife = null });

        Assert.True(result.IsValid);
    }
}
