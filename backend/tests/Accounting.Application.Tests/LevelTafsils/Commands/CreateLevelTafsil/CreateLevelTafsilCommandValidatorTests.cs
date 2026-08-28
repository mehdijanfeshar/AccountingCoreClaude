using Accounting.Application.LevelTafsils.Commands.CreateLevelTafsil;

namespace Accounting.Application.Tests.LevelTafsils.Commands.CreateLevelTafsil;

public sealed class CreateLevelTafsilCommandValidatorTests
{
    private readonly CreateLevelTafsilCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(new CreateLevelTafsilCommand("01", "سطح یک"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyLevelCode_Fails()
    {
        var result = _validator.Validate(new CreateLevelTafsilCommand("", "سطح یک"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateLevelTafsilCommand.LevelCode));
    }

    [Fact]
    public void Validate_LevelCodeTooLong_Fails()
    {
        var result = _validator.Validate(new CreateLevelTafsilCommand("001", "سطح یک"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateLevelTafsilCommand.LevelCode));
    }

    [Fact]
    public void Validate_EmptyLevelName_Fails()
    {
        var result = _validator.Validate(new CreateLevelTafsilCommand("01", ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateLevelTafsilCommand.LevelName));
    }

    [Fact]
    public void Validate_LevelNameTooLong_Fails()
    {
        var result = _validator.Validate(new CreateLevelTafsilCommand("01", new string('a', 51)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateLevelTafsilCommand.LevelName));
    }

    [Fact]
    public void Validate_LevelNameAtMaxLength_Passes()
    {
        var result = _validator.Validate(new CreateLevelTafsilCommand("01", new string('a', 50)));

        Assert.True(result.IsValid);
    }
}
