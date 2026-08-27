using Accounting.Application.Rabets.Commands.UpdateRabet;

namespace Accounting.Application.Tests.Rabets.Commands.UpdateRabet;

public sealed class UpdateRabetCommandValidatorTests
{
    private readonly UpdateRabetCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(new UpdateRabetCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new UpdateRabetCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateRabetCommand.Id));
    }
}
