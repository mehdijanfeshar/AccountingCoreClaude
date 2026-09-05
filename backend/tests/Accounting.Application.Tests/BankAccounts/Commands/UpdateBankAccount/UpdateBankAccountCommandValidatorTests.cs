using Accounting.Application.BankAccounts.Commands.UpdateBankAccount;

namespace Accounting.Application.Tests.BankAccounts.Commands.UpdateBankAccount;

public sealed class UpdateBankAccountCommandValidatorTests
{
    private readonly UpdateBankAccountCommandValidator _validator = new();

    private static UpdateBankAccountCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        AccountNumber: "0987654321",
        AccountHolder: "مریم احمدی",
        CardNumber: "6104331234567890",
        ShebaNumber: "IR120170000000987654321098",
        FirstAmount: 2_000_000m,
        BankId: Guid.NewGuid(),
        BranchId: Guid.NewGuid(),
        AccountTypeId: Guid.NewGuid(),
        AccountCodeId: Guid.NewGuid(),
        CheckFile: null,
        VahedCode: "0002",
        AccountOpeningDate: "13990202");

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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankAccountCommand.Id));
    }

    [Fact]
    public void Validate_AllOptionalsNull_Passes()
    {
        var result = _validator.Validate(ValidCommand() with
        {
            CardNumber = null,
            ShebaNumber = null,
            FirstAmount = null,
            BankId = null,
            BranchId = null,
            AccountTypeId = null,
            AccountCodeId = null,
            VahedCode = null,
            AccountOpeningDate = null,
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyAccountNumber_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountNumber = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankAccountCommand.AccountNumber));
    }

    [Fact]
    public void Validate_AccountNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountNumber = new string('1', 16) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankAccountCommand.AccountNumber));
    }

    [Fact]
    public void Validate_EmptyAccountHolder_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountHolder = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankAccountCommand.AccountHolder));
    }

    [Fact]
    public void Validate_AccountHolderTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountHolder = new string('a', 81) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankAccountCommand.AccountHolder));
    }

    [Fact]
    public void Validate_CardNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CardNumber = new string('1', 17) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankAccountCommand.CardNumber));
    }

    [Fact]
    public void Validate_ShebaNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ShebaNumber = new string('1', 51) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankAccountCommand.ShebaNumber));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00002" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankAccountCommand.VahedCode));
    }

    [Fact]
    public void Validate_AccountOpeningDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountOpeningDate = "139902021" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateBankAccountCommand.AccountOpeningDate));
    }
}
