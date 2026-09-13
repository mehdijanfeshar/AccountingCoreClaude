using Accounting.Application.Tafsilis.Commands.CreateTafsili;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.Tafsilis.Commands.CreateTafsili;

public sealed class CreateTafsiliCommandValidatorTests
{
    private readonly CreateTafsiliCommandValidator _validator = new();

    private static CreateTafsiliCommand ValidCommand(
        string code = "001",
        string name = "تفصیلی یک",
        string? desc = null,
        VahedCategory? tafsilGroupLinkVahedType = null) => new(
            code, name, desc, true, null, null, null, Array.Empty<Guid>(), tafsilGroupLinkVahedType);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyTafsiliCode_Fails()
    {
        var result = _validator.Validate(ValidCommand(code: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsiliCommand.TafsiliCode));
    }

    [Fact]
    public void Validate_TafsiliCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand(code: new string('1', 16)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsiliCommand.TafsiliCode));
    }

    [Fact]
    public void Validate_TafsiliCodeAtMaxLength_Passes()
    {
        var result = _validator.Validate(ValidCommand(code: new string('1', 15)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyTafsiliName_Fails()
    {
        var result = _validator.Validate(ValidCommand(name: ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsiliCommand.TafsiliName));
    }

    [Fact]
    public void Validate_TafsiliNameTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand(name: new string('a', 201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsiliCommand.TafsiliName));
    }

    [Fact]
    public void Validate_TafsilDescTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand(desc: new string('a', 201)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsiliCommand.TafsilDesc));
    }

    [Fact]
    public void Validate_NullTafsilDesc_Passes()
    {
        var result = _validator.Validate(ValidCommand(desc: null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullTafsilGroupLinkVahedType_Passes()
    {
        var result = _validator.Validate(ValidCommand(tafsilGroupLinkVahedType: null));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(VahedCategory.Insurance)]
    [InlineData(VahedCategory.Treatment)]
    [InlineData(VahedCategory.All)]
    public void Validate_DefinedTafsilGroupLinkVahedType_Passes(VahedCategory category)
    {
        var result = _validator.Validate(ValidCommand(tafsilGroupLinkVahedType: category));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_UndefinedTafsilGroupLinkVahedType_Fails()
    {
        var result = _validator.Validate(ValidCommand(tafsilGroupLinkVahedType: (VahedCategory)99));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsiliCommand.TafsilGroupLinkVahedType));
    }
}
