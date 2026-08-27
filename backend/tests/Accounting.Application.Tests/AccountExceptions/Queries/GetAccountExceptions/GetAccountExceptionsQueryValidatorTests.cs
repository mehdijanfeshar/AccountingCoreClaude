using Accounting.Application.AccountExceptions.Queries.GetAccountExceptions;

namespace Accounting.Application.Tests.AccountExceptions.Queries.GetAccountExceptions;

public sealed class GetAccountExceptionsQueryValidatorTests
{
    private readonly GetAccountExceptionsQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.Validate(new GetAccountExceptionsQuery(PageNumber: 1, PageSize: 20));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberLessThanOne_Fails()
    {
        var result = _validator.Validate(new GetAccountExceptionsQuery(PageNumber: 0, PageSize: 20));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAccountExceptionsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageSizeAboveMax_Fails()
    {
        var result = _validator.Validate(new GetAccountExceptionsQuery(PageNumber: 1, PageSize: GetAccountExceptionsQueryValidator.MaxPageSize + 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAccountExceptionsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAtMax_Passes()
    {
        var result = _validator.Validate(new GetAccountExceptionsQuery(PageNumber: 1, PageSize: GetAccountExceptionsQueryValidator.MaxPageSize));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageSizeZero_Fails()
    {
        var result = _validator.Validate(new GetAccountExceptionsQuery(PageNumber: 1, PageSize: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAccountExceptionsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageNumberAboveMax_Fails()
    {
        var result = _validator.Validate(new GetAccountExceptionsQuery(PageNumber: 10_737_420, PageSize: 200));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAccountExceptionsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageNumberAtMax_Passes()
    {
        var result = _validator.Validate(new GetAccountExceptionsQuery(PageNumber: GetAccountExceptionsQueryValidator.MaxPageNumber, PageSize: 20));

        Assert.True(result.IsValid);
    }
}
