using Accounting.Application.BankCartDetails.Commands.CreateBankCartDetail;

namespace Accounting.Application.Tests.BankCartDetails.Commands.CreateBankCartDetail;

public sealed class CreateBankCartDetailCommandValidatorTests
{
    private readonly CreateBankCartDetailCommandValidator _validator = new();

    private static CreateBankCartDetailCommand ValidCommand() => new(
        ReceipId: Guid.NewGuid(),
        CheckId: Guid.NewGuid(),
        BankId: Guid.NewGuid(),
        BranchId: Guid.NewGuid(),
        AccountNumber: "1234567890123",
        Month: "01",
        Cheqno: "12345678",
        RecivDate: "14020101",
        CheckReceiptType: true,
        Debtor: 1000m,
        Creditor: 0m,
        VahedCode: "0001",
        Year: "1402",
        CheckIncorrentId: Guid.NewGuid());

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(ValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AllOptionalFieldsNull_Passes()
    {
        var command = new CreateBankCartDetailCommand(
            ReceipId: null,
            CheckId: null,
            BankId: null,
            BranchId: null,
            AccountNumber: null,
            Month: null,
            Cheqno: null,
            RecivDate: null,
            CheckReceiptType: null,
            Debtor: null,
            Creditor: null,
            VahedCode: null,
            Year: null,
            CheckIncorrentId: null);

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_AccountNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountNumber = new string('1', 14) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankCartDetailCommand.AccountNumber));
    }

    [Fact]
    public void Validate_MonthTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Month = "123" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankCartDetailCommand.Month));
    }

    [Fact]
    public void Validate_CheqnoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Cheqno = new string('1', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankCartDetailCommand.Cheqno));
    }

    [Fact]
    public void Validate_RecivDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { RecivDate = new string('1', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankCartDetailCommand.RecivDate));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankCartDetailCommand.VahedCode));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = "14021" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankCartDetailCommand.Year));
    }
}
