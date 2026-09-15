using Accounting.Application.AccountCodeInterfaces.Commands.UpdateAccountCodeInterface;

namespace Accounting.Application.Tests.AccountCodeInterfaces.Commands.UpdateAccountCodeInterface;

public sealed class UpdateAccountCodeInterfaceCommandValidatorTests
{
    private readonly UpdateAccountCodeInterfaceCommandValidator _validator = new();

    private static UpdateAccountCodeInterfaceCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        Type: true,
        AccountCodeId: Guid.NewGuid());

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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountCodeInterfaceCommand.Id));
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var command = ValidCommand() with { AccountCodeId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAccountCodeInterfaceCommand.AccountCodeId));
    }
}
