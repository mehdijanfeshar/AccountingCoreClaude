using Accounting.Application.TmpVoucherHeads.Commands.UpdateTmpVoucherHead;

namespace Accounting.Application.Tests.TmpVoucherHeads.Commands.UpdateTmpVoucherHead;

public sealed class UpdateTmpVoucherHeadCommandValidatorTests
{
    private readonly UpdateTmpVoucherHeadCommandValidator _validator = new();

    private static UpdateTmpVoucherHeadCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
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

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        Assert.False(_validator.Validate(ValidCommand() with { Id = Guid.Empty }).IsValid);
    }

    /// <summary>
    /// The route-bound <c>Id</c> is the only required field; every business column is nullable,
    /// so a body that clears them all is syntactically valid (see the handler tests for what that
    /// actually does).
    /// </summary>
    [Fact]
    public void Validate_OnlyIdSupplied_Passes()
    {
        var command = new UpdateTmpVoucherHeadCommand(Guid.NewGuid(), null, null, null, null, null, null, null);

        Assert.True(_validator.Validate(command).IsValid);
    }

    /// <summary>
    /// The Update validator must stay rule-for-rule identical to the Create one on the shared
    /// fields — a drift between them would mean a value acceptable at creation becomes
    /// un-editable (or vice versa).
    /// </summary>
    [Fact]
    public void Validate_MaximumLengths_MatchCreateValidator()
    {
        Assert.False(_validator.Validate(ValidCommand() with { DateDoc = new string('1', 9) }).IsValid);
        Assert.False(_validator.Validate(ValidCommand() with { HeadDesc = new string('x', 251) }).IsValid);
        Assert.False(_validator.Validate(ValidCommand() with { VahedCode = new string('1', 5) }).IsValid);
        Assert.False(_validator.Validate(ValidCommand() with { Year = new string('1', 5) }).IsValid);
        Assert.False(_validator.Validate(ValidCommand() with { SysType = "AB" }).IsValid);

        Assert.True(_validator.Validate(ValidCommand() with { DateDoc = new string('1', 8) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { HeadDesc = new string('x', 250) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { VahedCode = new string('1', 4) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { Year = new string('1', 4) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { SysType = "A" }).IsValid);
    }
}
