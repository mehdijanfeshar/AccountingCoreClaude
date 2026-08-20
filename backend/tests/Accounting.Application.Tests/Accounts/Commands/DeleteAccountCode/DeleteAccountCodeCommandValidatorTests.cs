using Accounting.Application.Accounts.Commands.DeleteAccountCode;

namespace Accounting.Application.Tests.Accounts.Commands.DeleteAccountCode;

public sealed class DeleteAccountCodeCommandValidatorTests
{
    private readonly DeleteAccountCodeCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteAccountCodeCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteAccountCodeCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteAccountCodeCommand.Id));
    }
}
