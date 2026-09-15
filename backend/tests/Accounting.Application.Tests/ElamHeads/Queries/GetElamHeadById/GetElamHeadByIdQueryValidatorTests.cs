using Accounting.Application.ElamHeads.Queries.GetElamHeadById;

namespace Accounting.Application.Tests.ElamHeads.Queries.GetElamHeadById;

public sealed class GetElamHeadByIdQueryValidatorTests
{
    private readonly GetElamHeadByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetElamHeadByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetElamHeadByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetElamHeadByIdQuery.Id));
    }
}
