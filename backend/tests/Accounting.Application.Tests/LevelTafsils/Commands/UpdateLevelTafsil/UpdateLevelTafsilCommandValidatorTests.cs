using Accounting.Application.LevelTafsils.Commands.UpdateLevelTafsil;

namespace Accounting.Application.Tests.LevelTafsils.Commands.UpdateLevelTafsil;

public sealed class UpdateLevelTafsilCommandValidatorTests
{
    private readonly UpdateLevelTafsilCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(new UpdateLevelTafsilCommand(Guid.NewGuid(), "01", "سطح یک"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new UpdateLevelTafsilCommand(Guid.Empty, "01", "سطح یک"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateLevelTafsilCommand.Id));
    }

    [Fact]
    public void Validate_LevelCodeTooLong_Fails()
    {
        var result = _validator.Validate(new UpdateLevelTafsilCommand(Guid.NewGuid(), "001", "سطح یک"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateLevelTafsilCommand.LevelCode));
    }

    [Fact]
    public void Validate_LevelNameTooLong_Fails()
    {
        var result = _validator.Validate(new UpdateLevelTafsilCommand(Guid.NewGuid(), "01", new string('a', 51)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateLevelTafsilCommand.LevelName));
    }
}
