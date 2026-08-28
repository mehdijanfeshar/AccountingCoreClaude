using Accounting.Application.ChequeTypes.Queries.GetChequeTypeById;

namespace Accounting.Application.Tests.ChequeTypes.Queries.GetChequeTypeById;

public sealed class GetChequeTypeByIdQueryValidatorTests
{
    private readonly GetChequeTypeByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetChequeTypeByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetChequeTypeByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetChequeTypeByIdQuery.Id));
    }
}
