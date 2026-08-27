using Accounting.Application.AccountExceptions.Queries.GetAccountExceptionById;

namespace Accounting.Application.Tests.AccountExceptions.Queries.GetAccountExceptionById;

public sealed class GetAccountExceptionByIdQueryValidatorTests
{
    private readonly GetAccountExceptionByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetAccountExceptionByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetAccountExceptionByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetAccountExceptionByIdQuery.Id));
    }
}
