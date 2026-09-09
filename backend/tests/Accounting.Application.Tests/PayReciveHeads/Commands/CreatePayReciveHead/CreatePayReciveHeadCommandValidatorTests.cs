using Accounting.Application.PayReciveHeads.Commands.CreatePayReciveHead;

namespace Accounting.Application.Tests.PayReciveHeads.Commands.CreatePayReciveHead;

public sealed class CreatePayReciveHeadCommandValidatorTests
{
    private readonly CreatePayReciveHeadCommandValidator _validator = new();

    private static CreatePayReciveHeadCommand ValidCommand() => new(
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

    /// <summary>
    /// The five NOT NULL business columns must all be rejected when blank. These are not
    /// invented rules — each mirrors a NOT NULL column in the Oracle schema.
    /// </summary>
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

    [Fact]
    public void Validate_PayReciveCode_OverFiveChars_Fails()
    {
        Assert.False(_validator.Validate(ValidCommand() with { PayReciveCode = new string('1', 6) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { PayReciveCode = new string('1', 5) }).IsValid);
    }

    [Fact]
    public void Validate_PayReciveDate_OverEightChars_Fails()
    {
        Assert.False(_validator.Validate(ValidCommand() with { PayReciveDate = new string('1', 9) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { PayReciveDate = new string('1', 8) }).IsValid);
    }

    [Fact]
    public void Validate_PayReciveDescription_Over250Chars_Fails()
    {
        Assert.False(_validator.Validate(ValidCommand() with { PayReciveDescription = new string('x', 251) }).IsValid);
        Assert.True(_validator.Validate(ValidCommand() with { PayReciveDescription = new string('x', 250) }).IsValid);
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
    /// The optional columns must stay optional — no invented NotNull rules.
    /// </summary>
    [Fact]
    public void Validate_NullOptionalFields_Pass()
    {
        var command = ValidCommand() with { PayReciveType = null, VoucherHeadId = null };

        Assert.True(_validator.Validate(command).IsValid);
    }

    /// <summary>
    /// Documents a deliberate omission rather than an oversight: the reference project rejects a
    /// duplicate document number, but <c>TB_PAYRECIVHEAD</c> has no UNIQUE constraint behind
    /// that rule, so re-creating it here would be inventing a business rule (and would be
    /// race-prone). Two commands carrying the same <c>PayReciveCode</c> are both valid.
    /// </summary>
    [Fact]
    public void Validate_DuplicatePayReciveCode_IsNotRejected_NoUniqueConstraintExists()
    {
        var first = ValidCommand() with { PayReciveCode = "00001" };
        var second = ValidCommand() with { PayReciveCode = "00001" };

        Assert.True(_validator.Validate(first).IsValid);
        Assert.True(_validator.Validate(second).IsValid);
    }

    /// <summary>
    /// No range rule is invented for <c>PayReciveType</c>: its CLR type is already known to be
    /// wrong (see <c>CreatePayReciveHeadCommand</c> XML doc) and constraining it further would
    /// fabricate a rule on top of a defect.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void Validate_AnyPayReciveType_Passes(bool? payReciveType)
    {
        Assert.True(_validator.Validate(ValidCommand() with { PayReciveType = payReciveType }).IsValid);
    }
}
