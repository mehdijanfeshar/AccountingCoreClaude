using Accounting.Application.BankAccounts.Commands.DeleteBankAccount;

namespace Accounting.Application.Tests.BankAccounts.Commands.DeleteBankAccount;

public sealed class DeleteBankAccountCommandValidatorTests
{
    private readonly DeleteBankAccountCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteBankAccountCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteBankAccountCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteBankAccountCommand.Id));
    }
}
