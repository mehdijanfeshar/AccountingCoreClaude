using Accounting.Application.TmpVoucherHeads.Commands.CreateTmpVoucherHead;

namespace Accounting.Application.Tests.TmpVoucherHeads.Commands.CreateTmpVoucherHead;

public sealed class CreateTmpVoucherHeadCommandValidatorTests
{
    private readonly CreateTmpVoucherHeadCommandValidator _validator = new();

    private static CreateTmpVoucherHeadCommand ValidCommand() => new(
        VoucherHeadId: Guid.NewGuid(),
        DateDoc: "14040101",
        HeadDesc: "شرح سند موقت",
        VahedCode: "0001",
        Year: "1404",
        SysType: "K",
        SourceId: Guid.NewGuid());

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        Assert.True(_validator.Validate(ValidCommand()).IsValid);
    }

    /// <summary>
    /// Every column on this table is nullable, so an entirely empty command is valid. This is a
    /// deliberate consequence of the "Legacy fully replaces the rich model" decision — inventing
    /// required fields the schema does not have would be fabricating business rules.
    /// </summary>
    [Fact]
    public void Validate_AllFieldsNull_Passes()
    {
        var command = new CreateTmpVoucherHeadCommand(null, null, null, null, null, null, null);

        Assert.True(_validator.Validate(command).IsValid);
    }

    [Fact]
    public void Validate_DateDoc_OverEightChars_Fails()
    {
        Assert.False(_validator.Validate(ValidCommand() with { DateDoc = new string('1', 9) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { DateDoc = new string('1', 8) }).IsValid);
    }

    [Fact]
    public void Validate_HeadDesc_Over250Chars_Fails()
    {
        Assert.False(_validator.Validate(ValidCommand() with { HeadDesc = new string('x', 251) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { HeadDesc = new string('x', 250) }).IsValid);
    }

    [Fact]
    public void Validate_VahedCode_OverFourChars_Fails()
    {
        Assert.False(_validator.Validate(ValidCommand() with { VahedCode = new string('1', 5) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { VahedCode = new string('1', 4) }).IsValid);
    }

    [Fact]
    public void Validate_Year_OverFourChars_Fails()
    {
        Assert.False(_validator.Validate(ValidCommand() with { Year = new string('1', 5) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { Year = new string('1', 4) }).IsValid);
    }

    /// <summary>
    /// <c>SYS_TYPE</c> is a single-character column, so anything longer must be rejected — but no
    /// allowed-value rule is invented, because the permitted set is unknown.
    /// </summary>
    [Fact]
    public void Validate_SysType_OverOneChar_Fails()
    {
        Assert.False(_validator.Validate(ValidCommand() with { SysType = "AB" }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { SysType = "A" }).IsValid);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("9")]
    [InlineData("z")]
    [InlineData("")]
    public void Validate_AnySingleCharSysType_Passes_NoAllowedValueSetIsInvented(string sysType)
    {
        Assert.True(_validator.Validate(ValidCommand() with { SysType = sysType }).IsValid);
    }

    /// <summary>
    /// <c>SourceId</c> has no FK in Legacy, so an arbitrary GUID passes validation and would be
    /// written silently. Pinned here so the accepted gap is visible in the test suite rather than
    /// only in prose (see docs/open-decisions.md).
    /// </summary>
    [Fact]
    public void Validate_ArbitrarySourceId_Passes_NoForeignKeyExists()
    {
        Assert.True(_validator.Validate(ValidCommand() with { SourceId = Guid.NewGuid() }).IsValid);
    }
}
