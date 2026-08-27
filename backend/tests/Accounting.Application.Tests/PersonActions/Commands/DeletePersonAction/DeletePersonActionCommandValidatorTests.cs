using Accounting.Application.PersonActions.Commands.DeletePersonAction;

namespace Accounting.Application.Tests.PersonActions.Commands.DeletePersonAction;

public sealed class DeletePersonActionCommandValidatorTests
{
    private readonly DeletePersonActionCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeletePersonActionCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeletePersonActionCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeletePersonActionCommand.Id));
    }
}
