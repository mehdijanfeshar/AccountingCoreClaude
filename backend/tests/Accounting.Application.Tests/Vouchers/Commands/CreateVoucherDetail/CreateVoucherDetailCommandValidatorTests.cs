using Accounting.Application.Vouchers.Commands.CreateVoucherDetail;

namespace Accounting.Application.Tests.Vouchers.Commands.CreateVoucherDetail;

public sealed class CreateVoucherDetailCommandValidatorTests
{
    private readonly CreateVoucherDetailCommandValidator _validator = new();

    private static CreateVoucherDetailCommand ValidCommand() => new(
        VoucherHeadId: Guid.NewGuid(),
        AccountId: Guid.NewGuid(),
        ReceiptId: null,
        CheckId: null,
        LowLevelCodeId: null,
        EtebarId: null,
        Description: "ردیف اول",
        Radif: 1,
        Debtor: 1000m,
        Creditor: null,
        VahedCode: "0001",
        Year: "1405");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyVoucherHeadId_Fails()
    {
        var command = ValidCommand() with { VoucherHeadId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherDetailCommand.VoucherHeadId));
    }

    [Fact]
    public void Validate_DescriptionOverMaxLength_Fails()
    {
        var command = ValidCommand() with { Description = new string('a', 201) };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherDetailCommand.Description));
    }

    [Fact]
    public void Validate_DescriptionAtMaxLength_Passes()
    {
        var command = ValidCommand() with { Description = new string('a', 200) };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DescriptionNull_Passes()
    {
        var command = ValidCommand() with { Description = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_VahedCodeOverMaxLength_Fails()
    {
        var command = ValidCommand() with { VahedCode = "00001" }; // 5 chars, max is 4

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherDetailCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeNull_Passes()
    {
        var command = ValidCommand() with { VahedCode = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_YearOverMaxLength_Fails()
    {
        var command = ValidCommand() with { Year = "14050" }; // 5 chars, max is 4

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateVoucherDetailCommand.Year));
    }

    [Fact]
    public void Validate_YearNull_Passes()
    {
        var command = ValidCommand() with { Year = null };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AllOptionalFksNull_Passes()
    {
        var command = ValidCommand() with
        {
            AccountId = null,
            ReceiptId = null,
            CheckId = null,
            LowLevelCodeId = null,
            EtebarId = null,
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }
}
