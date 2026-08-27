using Accounting.Application.Rabets.Commands.DeleteRabet;

namespace Accounting.Application.Tests.Rabets.Commands.DeleteRabet;

public sealed class DeleteRabetCommandValidatorTests
{
    private readonly DeleteRabetCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteRabetCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteRabetCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteRabetCommand.Id));
    }
}
