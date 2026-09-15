using Accounting.Application.VahedInfos.Commands.CreateVahedInfo;

namespace Accounting.Application.Tests.VahedInfos.Commands.CreateVahedInfo;

public sealed class CreateVahedInfoCommandValidatorTests
{
    private readonly CreateVahedInfoCommandValidator _validator = new();

    private static CreateVahedInfoCommand ValidCommand() => new(
        VahedCode: "0001",
        VahedName: "واحد مرکزی",
        CityId: Guid.NewGuid(),
        VahedTypeId: Guid.NewGuid(),
        ParentId: null);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVahedInfoCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVahedInfoCommand.VahedCode));
    }

    [Fact]
    public void Validate_EmptyVahedName_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedName = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVahedInfoCommand.VahedName));
    }

    [Fact]
    public void Validate_VahedNameTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedName = new string('a', 201) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVahedInfoCommand.VahedName));
    }

    [Fact]
    public void Validate_EmptyCityId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CityId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVahedInfoCommand.CityId));
    }

    [Fact]
    public void Validate_EmptyVahedTypeId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedTypeId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVahedInfoCommand.VahedTypeId));
    }

    [Fact]
    public void Validate_NullParentId_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { ParentId = null });

        Assert.True(result.IsValid);
    }
}
