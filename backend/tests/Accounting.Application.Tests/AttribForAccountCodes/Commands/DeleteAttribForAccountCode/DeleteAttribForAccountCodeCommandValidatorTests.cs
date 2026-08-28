using Accounting.Application.AttribForAccountCodes.Commands.DeleteAttribForAccountCode;

namespace Accounting.Application.Tests.AttribForAccountCodes.Commands.DeleteAttribForAccountCode;

public sealed class DeleteAttribForAccountCodeCommandValidatorTests
{
    private readonly DeleteAttribForAccountCodeCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteAttribForAccountCodeCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteAttribForAccountCodeCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteAttribForAccountCodeCommand.Id));
    }
}
