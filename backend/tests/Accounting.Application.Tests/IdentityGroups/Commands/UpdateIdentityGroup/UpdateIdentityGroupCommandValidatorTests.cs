using Accounting.Application.IdentityGroups.Commands.UpdateIdentityGroup;

namespace Accounting.Application.Tests.IdentityGroups.Commands.UpdateIdentityGroup;

public sealed class UpdateIdentityGroupCommandValidatorTests
{
    private readonly UpdateIdentityGroupCommandValidator _validator = new();

    private static UpdateIdentityGroupCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        IdentityGroupsDesc: "Updated group",
        IdentityGroupsCode: "002",
        VahedCode: "0200",
        TafsiliId: Guid.NewGuid());

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Id = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateIdentityGroupCommand.Id));
    }

    [Fact]
    public void Validate_EmptyIdentityGroupsDesc_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { IdentityGroupsDesc = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateIdentityGroupCommand.IdentityGroupsDesc));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateIdentityGroupCommand.VahedCode));
    }
}
