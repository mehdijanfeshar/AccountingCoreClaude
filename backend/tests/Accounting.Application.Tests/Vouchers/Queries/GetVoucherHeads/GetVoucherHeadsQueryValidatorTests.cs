using Accounting.Application.Vouchers.Queries.GetVoucherHeads;

namespace Accounting.Application.Tests.Vouchers.Queries.GetVoucherHeads;

public sealed class GetVoucherHeadsQueryValidatorTests
{
    private readonly GetVoucherHeadsQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(PageNumber: 1, PageSize: 20, Year: "1405", VahedCode: "0001"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullFilters_Pass()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(PageNumber: 1, PageSize: 20, Year: null, VahedCode: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberLessThanOne_Fails()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(PageNumber: 0, PageSize: 20, Year: null, VahedCode: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherHeadsQuery.PageNumber));
    }

    [Fact]
    public void Validate_NegativePageNumber_Fails()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(PageNumber: -1, PageSize: 20, Year: null, VahedCode: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherHeadsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageSizeZero_Fails()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(PageNumber: 1, PageSize: 0, Year: null, VahedCode: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherHeadsQuery.PageSize));
    }

    /// <summary>
    /// QA-flagged fix: the repository computes <c>Skip((pageNumber - 1) * pageSize)</c> as
    /// plain <c>int</c> arithmetic (see <c>VoucherHeadReadRepository.GetPagedAsync</c>), which
    /// would overflow for large PageNumber/PageSize combinations (e.g. PageNumber=10_737_420,
    /// PageSize=200 wraps to a negative Skip count). The validator now enforces
    /// <see cref="GetVoucherHeadsQueryValidator.MaxPageNumber"/> as an upper bound, so such a
    /// PageNumber must be rejected.
    /// </summary>
    [Fact]
    public void Validate_PageNumberAboveMax_Fails()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(PageNumber: 10_737_420, PageSize: 200, Year: null, VahedCode: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherHeadsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageNumberAtMax_Passes()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(PageNumber: GetVoucherHeadsQueryValidator.MaxPageNumber, PageSize: 20, Year: null, VahedCode: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberJustAboveMax_Fails()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(PageNumber: GetVoucherHeadsQueryValidator.MaxPageNumber + 1, PageSize: 20, Year: null, VahedCode: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherHeadsQuery.PageNumber));
    }

    /// <summary>
    /// Proves the overflow-prevention invariant: at the worst allowed combination
    /// (PageNumber = MaxPageNumber, PageSize = MaxPageSize), the repository's
    /// <c>(pageNumber - 1) * pageSize</c> computation stays within <see cref="int"/> range and
    /// stays positive/zero (never wraps negative).
    /// </summary>
    [Fact]
    public void SkipComputation_AtMaxPageNumberAndMaxPageSize_DoesNotOverflow()
    {
        const int pageNumber = GetVoucherHeadsQueryValidator.MaxPageNumber;
        const int pageSize = GetVoucherHeadsQueryValidator.MaxPageSize;

        long expected = (long)(pageNumber - 1) * pageSize;
        int actual = (pageNumber - 1) * pageSize;

        Assert.Equal(expected, actual);
        Assert.True(actual >= 0);
        Assert.True(actual < int.MaxValue);
    }

    [Fact]
    public void Validate_YearAtMaxLength_Passes()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(PageNumber: 1, PageSize: 20, Year: "1405", VahedCode: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_VahedCodeAtMaxLength_Passes()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(PageNumber: 1, PageSize: 20, Year: null, VahedCode: "0001"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageSizeAboveMax_Fails()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(
            PageNumber: 1,
            PageSize: GetVoucherHeadsQueryValidator.MaxPageSize + 1,
            Year: null,
            VahedCode: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherHeadsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAtMax_Passes()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(
            PageNumber: 1,
            PageSize: GetVoucherHeadsQueryValidator.MaxPageSize,
            Year: null,
            VahedCode: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_YearOverMaxLength_Fails()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(PageNumber: 1, PageSize: 20, Year: "14050", VahedCode: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherHeadsQuery.Year));
    }

    [Fact]
    public void Validate_VahedCodeOverMaxLength_Fails()
    {
        var result = _validator.Validate(new GetVoucherHeadsQuery(PageNumber: 1, PageSize: 20, Year: null, VahedCode: "00011"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetVoucherHeadsQuery.VahedCode));
    }
}
