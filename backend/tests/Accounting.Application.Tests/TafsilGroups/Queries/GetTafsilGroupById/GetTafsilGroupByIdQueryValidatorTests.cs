using Accounting.Application.TafsilGroups.Queries.GetTafsilGroupById;

namespace Accounting.Application.Tests.TafsilGroups.Queries.GetTafsilGroupById;

public sealed class GetTafsilGroupByIdQueryValidatorTests
{
    private readonly GetTafsilGroupByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetTafsilGroupByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetTafsilGroupByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetTafsilGroupByIdQuery.Id));
    }
}
