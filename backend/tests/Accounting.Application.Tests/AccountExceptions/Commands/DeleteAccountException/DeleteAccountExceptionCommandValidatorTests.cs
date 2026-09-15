using Accounting.Application.AccountExceptions.Commands.DeleteAccountException;

namespace Accounting.Application.Tests.AccountExceptions.Commands.DeleteAccountException;

public sealed class DeleteAccountExceptionCommandValidatorTests
{
    private readonly DeleteAccountExceptionCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteAccountExceptionCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteAccountExceptionCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteAccountExceptionCommand.Id));
    }
}
