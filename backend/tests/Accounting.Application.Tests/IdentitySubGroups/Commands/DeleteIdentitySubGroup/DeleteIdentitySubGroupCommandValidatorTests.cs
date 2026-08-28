using Accounting.Application.IdentitySubGroups.Commands.DeleteIdentitySubGroup;

namespace Accounting.Application.Tests.IdentitySubGroups.Commands.DeleteIdentitySubGroup;

public sealed class DeleteIdentitySubGroupCommandValidatorTests
{
    private readonly DeleteIdentitySubGroupCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteIdentitySubGroupCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteIdentitySubGroupCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteIdentitySubGroupCommand.Id));
    }
}
