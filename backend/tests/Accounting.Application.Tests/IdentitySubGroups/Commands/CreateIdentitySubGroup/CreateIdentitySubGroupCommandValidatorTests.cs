using Accounting.Application.IdentitySubGroups.Commands.CreateIdentitySubGroup;

namespace Accounting.Application.Tests.IdentitySubGroups.Commands.CreateIdentitySubGroup;

public sealed class CreateIdentitySubGroupCommandValidatorTests
{
    private readonly CreateIdentitySubGroupCommandValidator _validator = new();

    private static CreateIdentitySubGroupCommand ValidCommand() => new(
        IdentyGroupsId: Guid.NewGuid(),
        SubgrpsDesc: "Sub group",
        SubgrpsLen: 4,
        SumFlag: true,
        Fixed: false,
        SubgrpsType: true,
        VahedCode: "0100",
        Year: "1403",
        IdentySubGroupsCode: "01");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyIdentyGroupsId_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { IdentyGroupsId = Guid.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateIdentitySubGroupCommand.IdentyGroupsId));
    }

    [Fact]
    public void Validate_EmptySubgrpsDesc_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { SubgrpsDesc = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateIdentitySubGroupCommand.SubgrpsDesc));
    }

    [Fact]
    public void Validate_SubgrpsDescTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { SubgrpsDesc = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateIdentitySubGroupCommand.SubgrpsDesc));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateIdentitySubGroupCommand.VahedCode));
    }

    [Fact]
    public void Validate_EmptyYear_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateIdentitySubGroupCommand.Year));
    }

    [Fact]
    public void Validate_IdentySubGroupsCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { IdentySubGroupsCode = "012" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateIdentitySubGroupCommand.IdentySubGroupsCode));
    }

    [Fact]
    public void Validate_NullSubgrpsTypeAndNullIdentySubGroupsCode_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { SubgrpsType = null, IdentySubGroupsCode = null });

        Assert.True(result.IsValid);
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateIdentitySubGroupCommand.SubgrpsLen));
    }

    [Fact]
    public void Validate_SubgrpsLenAt99_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { SubgrpsLen = 99 });

        Assert.True(result.IsValid);
    }
}
