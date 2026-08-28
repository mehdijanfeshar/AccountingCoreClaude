using Accounting.Application.WorkShops.Queries.GetWorkShopById;

namespace Accounting.Application.Tests.WorkShops.Queries.GetWorkShopById;

public sealed class GetWorkShopByIdQueryValidatorTests
{
    private readonly GetWorkShopByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetWorkShopByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetWorkShopByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetWorkShopByIdQuery.Id));
    }
}
