using Accounting.Application.IdentitySubGroups.Commands.UpdateIdentitySubGroup;

namespace Accounting.Application.Tests.IdentitySubGroups.Commands.UpdateIdentitySubGroup;

public sealed class UpdateIdentitySubGroupCommandValidatorTests
{
    private readonly UpdateIdentitySubGroupCommandValidator _validator = new();

    private static UpdateIdentitySubGroupCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        IdentyGroupsId: Guid.NewGuid(),
        SubgrpsDesc: "Updated sub group",
        SubgrpsLen: 6,
        SumFlag: false,
        Fixed: true,
        SubgrpsType: false,
        VahedCode: "0200",
        Year: "1404",
        IdentySubGroupsCode: "02");

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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateIdentitySubGroupCommand.Id));
    }

    [Fact]
    public void Validate_EmptyIdentyGroupsId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { IdentyGroupsId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateIdentitySubGroupCommand.IdentyGroupsId));
    }

    [Fact]
    public void Validate_EmptySubgrpsDesc_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { SubgrpsDesc = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateIdentitySubGroupCommand.SubgrpsDesc));
    }

    /// <summary>
    /// Regression test: SUBGRPS_LEN is Oracle NUMBER(2) (max 99), narrower than the CLR byte
    /// range (0-255). Without this rule, 150 would pass FluentValidation and only fail at
    /// SaveChangesAsync with a raw ORA-01438-driven 500 instead of a clean 400.
    /// </summary>
    [Fact]
    public void Validate_SubgrpsLenAbove99_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { SubgrpsLen = 150 });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateIdentitySubGroupCommand.SubgrpsLen));
    }

    [Fact]
    public void Validate_SubgrpsLenAt99_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { SubgrpsLen = 99 });

        Assert.True(result.IsValid);
    }
}
