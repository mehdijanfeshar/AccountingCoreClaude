using Accounting.Application.AccountCodeInterfaces.Commands.CreateAccountCodeInterface;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.AccountCodeInterfaces.Commands.CreateAccountCodeInterface;

public sealed class CreateAccountCodeInterfaceCommandValidatorTests
{
    private readonly CreateAccountCodeInterfaceCommandValidator _validator = new();

    private static CreateAccountCodeInterfaceCommand ValidCommand() => new(
        Type: InterfaceType.OpenVoucher,
        AccountCodeId: Guid.NewGuid());

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountCodeId_Fails()
    {
        var command = ValidCommand() with { AccountCodeId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeInterfaceCommand.AccountCodeId));
    }

    [Fact]
    public void Validate_InvalidTypeEnumValue_Fails()
    {
        var command = ValidCommand() with { Type = (InterfaceType)999 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAccountCodeInterfaceCommand.Type));
    }
}
