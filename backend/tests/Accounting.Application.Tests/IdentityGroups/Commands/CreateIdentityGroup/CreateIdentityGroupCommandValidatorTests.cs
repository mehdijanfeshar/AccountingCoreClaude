using Accounting.Application.IdentityGroups.Commands.CreateIdentityGroup;

namespace Accounting.Application.Tests.IdentityGroups.Commands.CreateIdentityGroup;

public sealed class CreateIdentityGroupCommandValidatorTests
{
    private readonly CreateIdentityGroupCommandValidator _validator = new();

    private static CreateIdentityGroupCommand ValidCommand() => new(
        IdentityGroupsDesc: "Main group",
        IdentityGroupsCode: "001",
        TafsiliId: Guid.NewGuid())
    {
        VahedCode = "0100",
    };

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyIdentityGroupsDesc_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { IdentityGroupsDesc = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateIdentityGroupCommand.IdentityGroupsDesc));
    }

    [Fact]
    public void Validate_IdentityGroupsDescTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { IdentityGroupsDesc = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateIdentityGroupCommand.IdentityGroupsDesc));
    }

    [Fact]
    public void Validate_IdentityGroupsCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { IdentityGroupsCode = "0001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateIdentityGroupCommand.IdentityGroupsCode));
    }

    [Fact]
    public void Validate_NullIdentityGroupsCode_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { IdentityGroupsCode = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateIdentityGroupCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateIdentityGroupCommand.VahedCode));
    }

    [Fact]
    public void Validate_NullTafsiliId_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { TafsiliId = null });

        Assert.True(result.IsValid);
    }
}
