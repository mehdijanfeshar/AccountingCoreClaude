using Accounting.Application.BankCartDetails.Queries.GetBankCartDetailById;

namespace Accounting.Application.Tests.BankCartDetails.Queries.GetBankCartDetailById;

public sealed class GetBankCartDetailByIdQueryValidatorTests
{
    private readonly GetBankCartDetailByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetBankCartDetailByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetBankCartDetailByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetBankCartDetailByIdQuery.Id));
    }
}
