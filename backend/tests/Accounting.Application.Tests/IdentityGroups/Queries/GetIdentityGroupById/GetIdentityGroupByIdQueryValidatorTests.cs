using Accounting.Application.IdentityGroups.Queries.GetIdentityGroupById;

namespace Accounting.Application.Tests.IdentityGroups.Queries.GetIdentityGroupById;

public sealed class GetIdentityGroupByIdQueryValidatorTests
{
    private readonly GetIdentityGroupByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetIdentityGroupByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetIdentityGroupByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetIdentityGroupByIdQuery.Id));
    }
}
