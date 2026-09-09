using Accounting.Application.BankCartDetails.Commands.UpdateBankCartDetail;

namespace Accounting.Application.Tests.BankCartDetails.Commands.UpdateBankCartDetail;

public sealed class UpdateBankCartDetailCommandValidatorTests
{
    private readonly UpdateBankCartDetailCommandValidator _validator = new();

    private static UpdateBankCartDetailCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        ReceipId: Guid.NewGuid(),
        CheckId: Guid.NewGuid(),
        BankId: Guid.NewGuid(),
        BranchId: Guid.NewGuid(),
        AccountNumber: "9876543210987",
        Month: "02",
        Cheqno: "87654321",
        RecivDate: "14020202",
        CheckReceiptType: false,
        Debtor: 0m,
        Creditor: 2000m,
        Year: "1403",
        CheckIncorrentId: Guid.NewGuid())
    {
        VahedCode = "0002",
    };

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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankCartDetailCommand.Id));
    }

    [Fact]
    public void Validate_AllOptionalFieldsNull_Passes()
    {
        // VahedCode is deliberately excluded from this "all optionals null" case: it is no
        // longer a client-optional field — VahedScopeBehavior always assigns a real,
        // non-empty value before this validator runs (see
        // UpdateBankCartDetailCommand.VahedCode XML doc), so NotEmpty() on it is correct even
        // though every other field here is genuinely nullable in Legacy.
        var command = new UpdateBankCartDetailCommand(
            Id: Guid.NewGuid(),
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
            Year: null,
            CheckIncorrentId: null)
        {
            VahedCode = "0002",
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankCartDetailCommand.VahedCode));
    }

    [Fact]
    public void Validate_AccountNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountNumber = new string('1', 14) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankCartDetailCommand.AccountNumber));
    }

    [Fact]
    public void Validate_MonthTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Month = "123" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankCartDetailCommand.Month));
    }

    [Fact]
    public void Validate_CheqnoTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Cheqno = new string('1', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankCartDetailCommand.Cheqno));
    }

    [Fact]
    public void Validate_RecivDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { RecivDate = new string('1', 9) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankCartDetailCommand.RecivDate));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00002" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankCartDetailCommand.VahedCode));
    }

    [Fact]
    public void Validate_YearTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Year = "14031" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankCartDetailCommand.Year));
    }
}
