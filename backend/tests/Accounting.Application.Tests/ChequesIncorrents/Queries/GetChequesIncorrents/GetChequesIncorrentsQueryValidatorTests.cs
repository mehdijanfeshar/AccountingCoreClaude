using Accounting.Application.ChequesIncorrents.Queries.GetChequesIncorrents;

namespace Accounting.Application.Tests.ChequesIncorrents.Queries.GetChequesIncorrents;

public sealed class GetChequesIncorrentsQueryValidatorTests
{
    private readonly GetChequesIncorrentsQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.Validate(new GetChequesIncorrentsQuery(PageNumber: 1, PageSize: 20));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberLessThanOne_Fails()
    {
        var result = _validator.Validate(new GetChequesIncorrentsQuery(PageNumber: 0, PageSize: 20));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetChequesIncorrentsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageSizeAboveMax_Fails()
    {
        var result = _validator.Validate(new GetChequesIncorrentsQuery(PageNumber: 1, PageSize: GetChequesIncorrentsQueryValidator.MaxPageSize + 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetChequesIncorrentsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAtMax_Passes()
    {
        var result = _validator.Validate(new GetChequesIncorrentsQuery(PageNumber: 1, PageSize: GetChequesIncorrentsQueryValidator.MaxPageSize));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageSizeZero_Fails()
    {
        var result = _validator.Validate(new GetChequesIncorrentsQuery(PageNumber: 1, PageSize: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetChequesIncorrentsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageNumberAboveMax_Fails()
    {
        var result = _validator.Validate(new GetChequesIncorrentsQuery(PageNumber: 10_737_420, PageSize: 200));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetChequesIncorrentsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageNumberAtMax_Passes()
    {
        var result = _validator.Validate(new GetChequesIncorrentsQuery(PageNumber: GetChequesIncorrentsQueryValidator.MaxPageNumber, PageSize: 20));

        Assert.True(result.IsValid);
    }
}
