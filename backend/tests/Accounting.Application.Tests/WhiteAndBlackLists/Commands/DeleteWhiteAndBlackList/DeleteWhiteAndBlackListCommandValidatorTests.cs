using Accounting.Application.WhiteAndBlackLists.Commands.DeleteWhiteAndBlackList;

namespace Accounting.Application.Tests.WhiteAndBlackLists.Commands.DeleteWhiteAndBlackList;

public sealed class DeleteWhiteAndBlackListCommandValidatorTests
{
    private readonly DeleteWhiteAndBlackListCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteWhiteAndBlackListCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteWhiteAndBlackListCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteWhiteAndBlackListCommand.Id));
    }
}
