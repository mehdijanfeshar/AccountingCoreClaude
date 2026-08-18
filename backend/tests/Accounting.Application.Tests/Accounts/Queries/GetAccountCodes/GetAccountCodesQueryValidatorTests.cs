using Accounting.Application.Accounts.Queries.GetAccountCodes;

namespace Accounting.Application.Tests.Accounts.Queries.GetAccountCodes;

public sealed class GetAccountCodesQueryValidatorTests
{
    private readonly GetAccountCodesQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.Validate(new GetAccountCodesQuery(PageNumber: 1, PageSize: 20));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberLessThanOne_Fails()
    {
        var result = _validator.Validate(new GetAccountCodesQuery(PageNumber: 0, PageSize: 20));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAccountCodesQuery.PageNumber));
    }

    [Fact]
    public void Validate_NegativePageNumber_Fails()
    {
        var result = _validator.Validate(new GetAccountCodesQuery(PageNumber: -1, PageSize: 20));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAccountCodesQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageSizeAboveMax_Fails()
    {
        var result = _validator.Validate(new GetAccountCodesQuery(PageNumber: 1, PageSize: GetAccountCodesQueryValidator.MaxPageSize + 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAccountCodesQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAtMax_Passes()
    {
        var result = _validator.Validate(new GetAccountCodesQuery(PageNumber: 1, PageSize: GetAccountCodesQueryValidator.MaxPageSize));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageSizeZero_Fails()
    {
        var result = _validator.Validate(new GetAccountCodesQuery(PageNumber: 1, PageSize: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAccountCodesQuery.PageSize));
    }

    /// <summary>
    /// QA-flagged fix: the repository computes <c>Skip((pageNumber - 1) * pageSize)</c> as
    /// plain <c>int</c> arithmetic (see <c>AccountCodeReadRepository.GetPagedAsync</c>), which
    /// would overflow for large PageNumber/PageSize combinations (e.g. PageNumber=10_737_420,
    /// PageSize=200 wraps to a negative Skip count). The validator now enforces
    /// <see cref="GetAccountCodesQueryValidator.MaxPageNumber"/> as an upper bound, so such a
    /// PageNumber must be rejected.
    /// </summary>
    [Fact]
    public void Validate_PageNumberAboveMax_Fails()
    {
        var result = _validator.Validate(new GetAccountCodesQuery(PageNumber: 10_737_420, PageSize: 200));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAccountCodesQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageNumberAtMax_Passes()
    {
        var result = _validator.Validate(new GetAccountCodesQuery(PageNumber: GetAccountCodesQueryValidator.MaxPageNumber, PageSize: 20));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberJustAboveMax_Fails()
    {
        var result = _validator.Validate(new GetAccountCodesQuery(PageNumber: GetAccountCodesQueryValidator.MaxPageNumber + 1, PageSize: 20));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAccountCodesQuery.PageNumber));
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
        const int pageNumber = GetAccountCodesQueryValidator.MaxPageNumber;
        const int pageSize = GetAccountCodesQueryValidator.MaxPageSize;

        long expected = (long)(pageNumber - 1) * pageSize;
        int actual = (pageNumber - 1) * pageSize;

        Assert.Equal(expected, actual);
        Assert.True(actual >= 0);
        Assert.True(actual < int.MaxValue);
    }
}
