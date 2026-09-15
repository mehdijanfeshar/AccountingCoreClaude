using Accounting.Application.AccountExceptions.Commands.UpdateAccountException;

namespace Accounting.Application.Tests.AccountExceptions.Commands.UpdateAccountException;

public sealed class UpdateAccountExceptionCommandValidatorTests
{
    private readonly UpdateAccountExceptionCommandValidator _validator = new();

    private static UpdateAccountExceptionCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        AccountCoeId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid());

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var command = ValidCommand() with { Id = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountExceptionCommand.Id));
    }

    [Fact]
    public void Validate_EmptyAccountCoeId_Fails()
    {
        var command = ValidCommand() with { AccountCoeId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountExceptionCommand.AccountCoeId));
    }

    [Fact]
    public void Validate_EmptyVahedTypeId_Fails()
    {
        var command = ValidCommand() with { VahedTypeId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountExceptionCommand.VahedTypeId));
    }
}
