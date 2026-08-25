using Accounting.Application.Vouchers.Queries.GetVoucherDetails;

namespace Accounting.Application.Tests.Vouchers.Queries.GetVoucherDetails;

public sealed class GetVoucherDetailsQueryValidatorTests
{
    private readonly GetVoucherDetailsQueryValidator _validator = new();

    private static GetVoucherDetailsQuery ValidQuery() => new(
        PageNumber: 1,
        PageSize: 20,
        VoucherHeadId: Guid.NewGuid(),
        Year: "1405",
        VahedCode: "0001");

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.Validate(ValidQuery());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullFilters_Pass()
    {
        var result = _validator.Validate(ValidQuery() with { VoucherHeadId = null, Year = null, VahedCode = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberLessThanOne_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { PageNumber = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherDetailsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageSizeZero_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { PageSize = 0 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherDetailsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAboveMax_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { PageSize = GetVoucherDetailsQueryValidator.MaxPageSize + 1 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherDetailsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAtMax_Passes()
    {
        var result = _validator.Validate(ValidQuery() with { PageSize = GetVoucherDetailsQueryValidator.MaxPageSize });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberAboveMax_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { PageNumber = GetVoucherDetailsQueryValidator.MaxPageNumber + 1 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherDetailsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageNumberAtMax_Passes()
    {
        var result = _validator.Validate(ValidQuery() with { PageNumber = GetVoucherDetailsQueryValidator.MaxPageNumber });

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Same overflow-prevention invariant proven for <c>GetVoucherHeadsQueryValidator</c>: at the
    /// worst allowed combination the repository's <c>(pageNumber - 1) * pageSize</c> computation
    /// stays within <see cref="int"/> range and never wraps negative.
    /// </summary>
    [Fact]
    public void SkipComputation_AtMaxPageNumberAndMaxPageSize_DoesNotOverflow()
    {
        const int pageNumber = GetVoucherDetailsQueryValidator.MaxPageNumber;
        const int pageSize = GetVoucherDetailsQueryValidator.MaxPageSize;

        long expected = (long)(pageNumber - 1) * pageSize;
        int actual = (pageNumber - 1) * pageSize;

        Assert.Equal(expected, actual);
        Assert.True(actual >= 0);
        Assert.True(actual < int.MaxValue);
    }

    [Fact]
    public void Validate_YearOverMaxLength_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { Year = "14050" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherDetailsQuery.Year));
    }

    [Fact]
    public void Validate_YearAtMaxLength_Passes()
    {
        var result = _validator.Validate(ValidQuery() with { Year = "1405" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_VahedCodeOverMaxLength_Fails()
    {
        var result = _validator.Validate(ValidQuery() with { VahedCode = "00011" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherDetailsQuery.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeAtMaxLength_Passes()
    {
        var result = _validator.Validate(ValidQuery() with { VahedCode = "0001" });

        Assert.True(result.IsValid);
    }
}
