using Accounting.Application.Receipts.Commands.UpdateReceipt;

namespace Accounting.Application.Tests.Receipts.Commands.UpdateReceipt;

public sealed class UpdateReceiptCommandValidatorTests
{
    private readonly UpdateReceiptCommandValidator _validator = new();

    private static UpdateReceiptCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        ReceiptKind: false,
        ReceiptDate: "14030101",
        ReceiptNo: "R0000002",
        DateRsid: "14030102",
        VahedCode: "0002",
        Year: "1403");

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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateReceiptCommand.Id));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateReceiptCommand.ReceiptDate));
    }

    [Fact]
    public void Validate_ReceiptDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ReceiptDate = new string('1', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateReceiptCommand.ReceiptDate));
    }

    [Fact]
    public void Validate_EmptyReceiptNo_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ReceiptNo = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateReceiptCommand.ReceiptNo));
    }

    [Fact]
    public void Validate_ReceiptNoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ReceiptNo = new string('1', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateReceiptCommand.ReceiptNo));
    }

    [Fact]
    public void Validate_DateRsidTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { DateRsid = new string('1', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateReceiptCommand.DateRsid));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateReceiptCommand.VahedCode));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00002" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateReceiptCommand.VahedCode));
    }

    [Fact]
    public void Validate_EmptyYear_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateReceiptCommand.Year));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = "14031" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateReceiptCommand.Year));
    }
}
