using Accounting.Application.WhiteLists.Commands.DeleteWhiteList;

namespace Accounting.Application.Tests.WhiteLists.Commands.DeleteWhiteList;

public sealed class DeleteWhiteListCommandValidatorTests
{
    private readonly DeleteWhiteListCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteWhiteListCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteWhiteListCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteWhiteListCommand.Id));
    }
}
