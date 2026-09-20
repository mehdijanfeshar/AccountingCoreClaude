using Accounting.Domain.ValueObjects;
using Accounting.Application.AttribForAccountCodes.Queries.GetAttribForAccountCodes;

namespace Accounting.Application.Tests.AttribForAccountCodes.Queries.GetAttribForAccountCodes;

public sealed class GetAttribForAccountCodesQueryValidatorTests
{
    private readonly GetAttribForAccountCodesQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.Validate(new GetAttribForAccountCodesQuery(PageNumber: 1, PageSize: 20));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberLessThanOne_Fails()
    {
        var result = _validator.Validate(new GetAttribForAccountCodesQuery(PageNumber: 0, PageSize: 20));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAttribForAccountCodesQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageSizeAboveMax_Fails()
    {
        var result = _validator.Validate(new GetAttribForAccountCodesQuery(PageNumber: 1, PageSize: GetAttribForAccountCodesQueryValidator.MaxPageSize + 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAttribForAccountCodesQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAtMax_Passes()
    {
        var result = _validator.Validate(new GetAttribForAccountCodesQuery(PageNumber: 1, PageSize: GetAttribForAccountCodesQueryValidator.MaxPageSize));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageSizeZero_Fails()
    {
        var result = _validator.Validate(new GetAttribForAccountCodesQuery(PageNumber: 1, PageSize: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAttribForAccountCodesQuery.PageSize));
    }

    [Fact]
    public void Validate_PageNumberAboveMax_Fails()
    {
        var result = _validator.Validate(new GetAttribForAccountCodesQuery(PageNumber: 10_737_420, PageSize: 200));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAttribForAccountCodesQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageNumberAtMax_Passes()
    {
        var result = _validator.Validate(new GetAttribForAccountCodesQuery(PageNumber: GetAttribForAccountCodesQueryValidator.MaxPageNumber, PageSize: 20));

        Assert.True(result.IsValid);
    }

    // --- Search-panel filters (parity with the reference app's base-identity-account) ----------

    private static GetAttribForAccountCodesQuery ValidQuery() => new(PageNumber: 1, PageSize: 20);

    /// <summary>
    /// Every filter is optional: omitting all of them is the default "show everything in my unit"
    /// case and must stay valid.
    /// </summary>
    [Fact]
    public void Validate_NoFiltersSupplied_Passes()
    {
        var result = _validator.Validate(ValidQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AllFiltersSupplied_Passes()
    {
        var result = _validator.Validate(ValidQuery() with
        {
            MoinCodeFrom = "110101",
            MoinCodeTo = "990909",
            AttribSum = AttribSum.Summable,
            Flag = AttribFlag.Date,
            Year = "1404",
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MoinCodeFromTooLong_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { MoinCodeFrom = "1101010" }); // 7 > 6

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAttribForAccountCodesQuery.MoinCodeFrom));
    }

    [Fact]
    public void Validate_MoinCodeToTooLong_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { MoinCodeTo = "1101010" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAttribForAccountCodesQuery.MoinCodeTo));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { Year = "14045" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAttribForAccountCodesQuery.Year));
    }

    /// <summary>
    /// An inverted range silently returns nothing, which reads as "no data" rather than "you
    /// filled the fields the wrong way round" — so it is rejected instead.
    /// </summary>
    [Fact]
    public void Validate_InvertedMoinCodeRange_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { MoinCodeFrom = "990909", MoinCodeTo = "110101" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAttribForAccountCodesQuery.MoinCodeTo));
    }

    [Fact]
    public void Validate_EqualMoinCodeBounds_Passes()
    {
        var result = _validator.Validate(ValidQuery() with { MoinCodeFrom = "110101", MoinCodeTo = "110101" });

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Only one end supplied must never trip the inverted-range rule — the other end is simply
    /// open.
    /// </summary>
    [Theory]
    [InlineData("110101", null)]
    [InlineData(null, "110101")]
    public void Validate_OneSidedMoinCodeRange_Passes(string? from, string? to)
    {
        var result = _validator.Validate(ValidQuery() with { MoinCodeFrom = from, MoinCodeTo = to });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AttribSumOutOfRange_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { AttribSum = (AttribSum)99 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAttribForAccountCodesQuery.AttribSum));
    }

    [Fact]
    public void Validate_FlagOutOfRange_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { Flag = (AttribFlag)99 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAttribForAccountCodesQuery.Flag));
    }
}
