using Accounting.Application.AccountExceptions.Commands.CreateAccountException;

namespace Accounting.Application.Tests.AccountExceptions.Commands.CreateAccountException;

public sealed class CreateAccountExceptionCommandValidatorTests
{
    private readonly CreateAccountExceptionCommandValidator _validator = new();

    private static CreateAccountExceptionCommand ValidCommand() => new(
        AccountCoeId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid());

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountCoeId_Fails()
    {
        var command = ValidCommand() with { AccountCoeId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountExceptionCommand.AccountCoeId));
    }

    [Fact]
    public void Validate_EmptyVahedTypeId_Fails()
    {
        var command = ValidCommand() with { VahedTypeId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountExceptionCommand.VahedTypeId));
    }
}
