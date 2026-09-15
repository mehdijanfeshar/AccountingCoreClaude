using Accounting.Application.IdentitySubGroups.Queries.GetIdentitySubGroups;

namespace Accounting.Application.Tests.IdentitySubGroups.Queries.GetIdentitySubGroups;

public sealed class GetIdentitySubGroupsQueryValidatorTests
{
    private readonly GetIdentitySubGroupsQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_Passes()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: 1, PageSize: 20));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageNumberLessThanOne_Fails()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: 0, PageSize: 20));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetIdentitySubGroupsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageSizeAboveMax_Fails()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: 1, PageSize: GetIdentitySubGroupsQueryValidator.MaxPageSize + 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetIdentitySubGroupsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageSizeAtMax_Passes()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: 1, PageSize: GetIdentitySubGroupsQueryValidator.MaxPageSize));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PageSizeZero_Fails()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: 1, PageSize: 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetIdentitySubGroupsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageNumberAboveMax_Fails()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: 10_737_420, PageSize: 200));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetIdentitySubGroupsQuery.PageNumber));
    }

    [Fact]
    public void Validate_PageNumberAtMax_Passes()
    {
        var result = _validator.Validate(new GetIdentitySubGroupsQuery(PageNumber: GetIdentitySubGroupsQueryValidator.MaxPageNumber, PageSize: 20));

        Assert.True(result.IsValid);
    }
}
