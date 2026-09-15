using Accounting.Application.AccountCodeInterfaces.Queries.GetAccountCodeInterfaceById;

namespace Accounting.Application.Tests.AccountCodeInterfaces.Queries.GetAccountCodeInterfaceById;

public sealed class GetAccountCodeInterfaceByIdQueryValidatorTests
{
    private readonly GetAccountCodeInterfaceByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetAccountCodeInterfaceByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetAccountCodeInterfaceByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAccountCodeInterfaceByIdQuery.Id));
    }
}
