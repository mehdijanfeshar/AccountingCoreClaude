using Accounting.Application.Tafsilis.Commands.UpdateTafsili;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Tests.Tafsilis.Commands.UpdateTafsili;

public sealed class UpdateTafsiliCommandValidatorTests
{
    private readonly UpdateTafsiliCommandValidator _validator = new();

    private static UpdateTafsiliCommand ValidCommand(
        Guid? id = null,
        string code = "001",
        string name = "تفصیلی یک",
        VahedCategory? tafsilGroupLinkVahedType = null) =>
        new(id ?? Guid.NewGuid(), code, name, null, null, null, null, null, Array.Empty<Guid>(), tafsilGroupLinkVahedType);

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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTafsiliCommand.TafsilGroupLinkVahedType));
    }

    // --- ISACTIVE / PERSONTYPE / OWNER / VAHEDTYPE enum coverage (phase 27 batch 1) -----------

    [Fact]
    public void Validate_NullIsActivePersonTypeOwnerVahedType_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_IsActiveOutOfRange_Fails()
    {
        var command = ValidCommand() with { IsActive = (TafsiliActiveState)99 };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTafsiliCommand.IsActive));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTafsiliCommand.PersonType));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTafsiliCommand.Owner));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateTafsiliCommand.VahedType));
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
