using Accounting.Application.BankAccounts.Queries.GetBankAccountById;

namespace Accounting.Application.Tests.BankAccounts.Queries.GetBankAccountById;

public sealed class GetBankAccountByIdQueryValidatorTests
{
    private readonly GetBankAccountByIdQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyGuid_Passes()
    {
        var result = _validator.Validate(new GetBankAccountByIdQuery(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyGuid_Fails()
    {
        var result = _validator.Validate(new GetBankAccountByIdQuery(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetBankAccountByIdQuery.Id));
    }
}
