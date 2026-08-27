using Accounting.Application.WhiteLists.Queries.GetWhiteListById;

namespace Accounting.Application.Tests.WhiteLists.Queries.GetWhiteListById;

public sealed class GetWhiteListByIdQueryValidatorTests
{
    private readonly GetWhiteListByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetWhiteListByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetWhiteListByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetWhiteListByIdQuery.Id));
    }
}
