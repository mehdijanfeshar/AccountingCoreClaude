using Accounting.Application.Rabets.Queries.GetRabetById;

namespace Accounting.Application.Tests.Rabets.Queries.GetRabetById;

public sealed class GetRabetByIdQueryValidatorTests
{
    private readonly GetRabetByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetRabetByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetRabetByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetRabetByIdQuery.Id));
    }
}
