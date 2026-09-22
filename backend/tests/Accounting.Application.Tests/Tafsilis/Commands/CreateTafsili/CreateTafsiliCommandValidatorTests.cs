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
            code, name, desc, TafsiliActiveState.IsActive, null, null, null, Array.Empty<Guid>(), tafsilGroupLinkVahedType);

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

    // --- ISACTIVE / PERSONTYPE / OWNER / VAHEDTYPE enum coverage (phase 27 batch 1) -----------
    // All four enums start at 1 (there is no 0 member), which is precisely why bool? was the
    // wrong CLR type for them.

    [Fact]
    public void Validate_NullIsActivePersonTypeOwnerVahedType_Passes()
    {
        var command = ValidCommand() with { IsActive = null, PersonType = null, Owner = null, VahedType = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_IsActiveOutOfRange_Fails()
    {
        var command = ValidCommand() with { IsActive = (TafsiliActiveState)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsiliCommand.IsActive));
    }

    [Fact]
    public void Validate_IsActiveZero_Fails()
    {
        var command = ValidCommand() with { IsActive = (TafsiliActiveState)0 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsiliCommand.IsActive));
    }

    [Theory]
    [InlineData(TafsiliActiveState.IsActive)]
    [InlineData(TafsiliActiveState.DeActive)]
    public void Validate_DefinedIsActive_Passes(TafsiliActiveState state)
    {
        var command = ValidCommand() with { IsActive = state };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PersonTypeOutOfRange_Fails()
    {
        var command = ValidCommand() with { PersonType = (PersonTypes)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsiliCommand.PersonType));
    }

    [Theory]
    [InlineData(PersonTypes.Person)]
    [InlineData(PersonTypes.Legal)]
    [InlineData(PersonTypes.Other)]
    public void Validate_DefinedPersonType_Passes(PersonTypes personType)
    {
        var command = ValidCommand() with { PersonType = personType };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_OwnerOutOfRange_Fails()
    {
        var command = ValidCommand() with { Owner = (Owners)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsiliCommand.Owner));
    }

    [Theory]
    [InlineData(Owners.Global)]
    [InlineData(Owners.Unit)]
    public void Validate_DefinedOwner_Passes(Owners owner)
    {
        var command = ValidCommand() with { Owner = owner };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_VahedTypeOutOfRange_Fails()
    {
        var command = ValidCommand() with { VahedType = (VahedCategory)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTafsiliCommand.VahedType));
    }

    [Theory]
    [InlineData(VahedCategory.Insurance)]
    [InlineData(VahedCategory.Treatment)]
    [InlineData(VahedCategory.All)]
    public void Validate_DefinedVahedType_Passes(VahedCategory category)
    {
        var command = ValidCommand() with { VahedType = category };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
