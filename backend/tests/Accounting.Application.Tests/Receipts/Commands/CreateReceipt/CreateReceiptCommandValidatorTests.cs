using Accounting.Application.Receipts.Commands.CreateReceipt;

namespace Accounting.Application.Tests.Receipts.Commands.CreateReceipt;

public sealed class CreateReceiptCommandValidatorTests
{
    private readonly CreateReceiptCommandValidator _validator = new();

    private static CreateReceiptCommand ValidCommand() => new(
        ReceiptKind: true,
        ReceiptDate: "14020101",
        ReceiptNo: "R0000001",
        DateRsid: "14020102",
        Year: "1402")
    {
        VahedCode = "0001",
    };

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_NullDateRsid_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { DateRsid = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyReceiptDate_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ReceiptDate = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReceiptCommand.ReceiptDate));
    }

    [Fact]
    public void Validate_ReceiptDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ReceiptDate = new string('1', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReceiptCommand.ReceiptDate));
    }

    [Fact]
    public void Validate_EmptyReceiptNo_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ReceiptNo = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReceiptCommand.ReceiptNo));
    }

    [Fact]
    public void Validate_ReceiptNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ReceiptNo = new string('1', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReceiptCommand.ReceiptNo));
    }

    [Fact]
    public void Validate_DateRsidTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DateRsid = new string('1', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReceiptCommand.DateRsid));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReceiptCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReceiptCommand.VahedCode));
    }

    [Fact]
    public void Validate_EmptyYear_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReceiptCommand.Year));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = "14021" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReceiptCommand.Year));
    }
}
