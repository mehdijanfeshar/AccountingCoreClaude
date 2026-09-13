using Accounting.Application.Tafsilis.Commands.UpdateTafsili;

namespace Accounting.Application.Tests.Tafsilis.Commands.UpdateTafsili;

public sealed class UpdateTafsiliCommandValidatorTests
{
    private readonly UpdateTafsiliCommandValidator _validator = new();

    private static UpdateTafsiliCommand ValidCommand(
        Guid? id = null,
        string code = "001",
        string name = "تفصیلی یک") =>
        new(id ?? Guid.NewGuid(), code, name, null, null, null, null, null, Array.Empty<Guid>());

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(ValidCommand(id: Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTafsiliCommand.Id));
    }

    [Fact]
    public void Validate_EmptyTafsiliCode_Fails()
    {
        var result = _validator.Validate(ValidCommand(code: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTafsiliCommand.TafsiliCode));
    }

    [Fact]
    public void Validate_TafsiliCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand(code: new string('1', 16)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTafsiliCommand.TafsiliCode));
    }

    [Fact]
    public void Validate_EmptyTafsiliName_Fails()
    {
        var result = _validator.Validate(ValidCommand(name: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTafsiliCommand.TafsiliName));
    }
}
