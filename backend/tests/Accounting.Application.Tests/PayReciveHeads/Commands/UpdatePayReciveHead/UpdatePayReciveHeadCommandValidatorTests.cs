using Accounting.Application.PayReciveHeads.Commands.UpdatePayReciveHead;

namespace Accounting.Application.Tests.PayReciveHeads.Commands.UpdatePayReciveHead;

public sealed class UpdatePayReciveHeadCommandValidatorTests
{
    private readonly UpdatePayReciveHeadCommandValidator _validator = new();

    private static UpdatePayReciveHeadCommand ValidCommand() => new(
        Id: Guid.NewGuid(),
        PayReciveCode: "00123",
        PayReciveDate: "14040101",
        PayReciveDescription: "شرح سند",
        PayReciveType: true,
        Year: "1404",
        VoucherHeadId: Guid.NewGuid())
    {
        VahedCode = "0001",
    };

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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankRequiredStrings_Fail(string blank)
    {
        Assert.False(_validator.Validate(ValidCommand() with { PayReciveCode = blank }).IsValid);
        Assert.False(_validator.Validate(ValidCommand() with { PayReciveDate = blank }).IsValid);
        Assert.False(_validator.Validate(ValidCommand() with { PayReciveDescription = blank }).IsValid);
        Assert.False(_validator.Validate(ValidCommand() with { VahedCode = blank }).IsValid);
        Assert.False(_validator.Validate(ValidCommand() with { Year = blank }).IsValid);
    }

    /// <summary>
    /// The Update validator must stay rule-for-rule identical to the Create one on the shared
    /// fields — a drift between them would mean a value acceptable at creation becomes
    /// un-editable (or vice versa).
    /// </summary>
    [Fact]
    public void Validate_MaximumLengths_MatchCreateValidator()
    {
        Assert.False(_validator.Validate(ValidCommand() with { PayReciveCode = new string('1', 6) }).IsValid);
        Assert.False(_validator.Validate(ValidCommand() with { PayReciveDate = new string('1', 9) }).IsValid);
        Assert.False(_validator.Validate(ValidCommand() with { PayReciveDescription = new string('x', 251) }).IsValid);
        Assert.False(_validator.Validate(ValidCommand() with { VahedCode = new string('1', 5) }).IsValid);
        Assert.False(_validator.Validate(ValidCommand() with { Year = new string('1', 5) }).IsValid);

        Assert.True(_validator.Validate(ValidCommand() with { PayReciveCode = new string('1', 5) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { PayReciveDate = new string('1', 8) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { PayReciveDescription = new string('x', 250) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { VahedCode = new string('1', 4) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { Year = new string('1', 4) }).IsValid);
    }

    [Fact]
    public void Validate_NullOptionalFields_Pass()
    {
        var command = ValidCommand() with { PayReciveType = null, VoucherHeadId = null };

        Assert.True(_validator.Validate(command).IsValid);
    }
}
