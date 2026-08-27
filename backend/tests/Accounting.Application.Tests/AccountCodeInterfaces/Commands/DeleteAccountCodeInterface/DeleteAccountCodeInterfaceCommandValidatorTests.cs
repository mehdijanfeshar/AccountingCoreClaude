using Accounting.Application.AccountCodeInterfaces.Commands.DeleteAccountCodeInterface;

namespace Accounting.Application.Tests.AccountCodeInterfaces.Commands.DeleteAccountCodeInterface;

public sealed class DeleteAccountCodeInterfaceCommandValidatorTests
{
    private readonly DeleteAccountCodeInterfaceCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteAccountCodeInterfaceCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteAccountCodeInterfaceCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteAccountCodeInterfaceCommand.Id));
    }
}
