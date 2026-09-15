using Accounting.Application.IdentitySubGroups.Queries.GetIdentitySubGroupById;

namespace Accounting.Application.Tests.IdentitySubGroups.Queries.GetIdentitySubGroupById;

public sealed class GetIdentitySubGroupByIdQueryValidatorTests
{
    private readonly GetIdentitySubGroupByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetIdentitySubGroupByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetIdentitySubGroupByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetIdentitySubGroupByIdQuery.Id));
    }
}
