using Accounting.Application.BankCartDetails.Queries.GetBankCartDetails;

namespace Accounting.Application.Tests.BankCartDetails.Queries.GetBankCartDetails;

public sealed class GetBankCartDetailsQueryValidatorTests
{
    private readonly GetBankCartDetailsQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.Validate(new GetBankCartDetailsQuery(PageNumber: 1, PageSize: 20));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberLessThanOne_Fails()
    {
        var result = _validator.Validate(new GetBankCartDetailsQuery(PageNumber: 0, PageSize: 20));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetBankCartDetailsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageSizeAboveMax_Fails()
    {
        var result = _validator.Validate(new GetBankCartDetailsQuery(PageNumber: 1, PageSize: GetBankCartDetailsQueryValidator.MaxPageSize + 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetBankCartDetailsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAtMax_Passes()
    {
        var result = _validator.Validate(new GetBankCartDetailsQuery(PageNumber: 1, PageSize: GetBankCartDetailsQueryValidator.MaxPageSize));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageSizeZero_Fails()
    {
        var result = _validator.Validate(new GetBankCartDetailsQuery(PageNumber: 1, PageSize: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetBankCartDetailsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageNumberAboveMax_Fails()
    {
        var result = _validator.Validate(new GetBankCartDetailsQuery(PageNumber: 10_737_420, PageSize: 200));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetBankCartDetailsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageNumberAtMax_Passes()
    {
        var result = _validator.Validate(new GetBankCartDetailsQuery(PageNumber: GetBankCartDetailsQueryValidator.MaxPageNumber, PageSize: 20));

        Assert.True(result.IsValid);
    }
}
