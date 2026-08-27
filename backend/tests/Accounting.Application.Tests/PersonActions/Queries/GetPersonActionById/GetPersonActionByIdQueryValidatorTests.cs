using Accounting.Application.PersonActions.Queries.GetPersonActionById;

namespace Accounting.Application.Tests.PersonActions.Queries.GetPersonActionById;

public sealed class GetPersonActionByIdQueryValidatorTests
{
    private readonly GetPersonActionByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetPersonActionByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetPersonActionByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetPersonActionByIdQuery.Id));
    }
}
