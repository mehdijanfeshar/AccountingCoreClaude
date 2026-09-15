using Accounting.Application.LevelTafsils.Commands.DeleteLevelTafsil;

namespace Accounting.Application.Tests.LevelTafsils.Commands.DeleteLevelTafsil;

public sealed class DeleteLevelTafsilCommandValidatorTests
{
    private readonly DeleteLevelTafsilCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteLevelTafsilCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteLevelTafsilCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteLevelTafsilCommand.Id));
    }
}
