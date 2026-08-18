using Accounting.Application.Accounts.Queries.GetAccountCodeById;

namespace Accounting.Application.Tests.Accounts.Queries.GetAccountCodeById;

public sealed class GetAccountCodeByIdQueryValidatorTests
{
    private readonly GetAccountCodeByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetAccountCodeByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetAccountCodeByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAccountCodeByIdQuery.Id));
    }
}
