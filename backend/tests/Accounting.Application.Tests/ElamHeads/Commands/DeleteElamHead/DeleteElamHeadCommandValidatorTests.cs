using Accounting.Application.ElamHeads.Commands.DeleteElamHead;

namespace Accounting.Application.Tests.ElamHeads.Commands.DeleteElamHead;

public sealed class DeleteElamHeadCommandValidatorTests
{
    private readonly DeleteElamHeadCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteElamHeadCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteElamHeadCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteElamHeadCommand.Id));
    }
}
