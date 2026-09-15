using Accounting.Application.BankAccounts.Commands.CreateBankAccount;

namespace Accounting.Application.Tests.BankAccounts.Commands.CreateBankAccount;

public sealed class CreateBankAccountCommandValidatorTests
{
    private readonly CreateBankAccountCommandValidator _validator = new();

    private static CreateBankAccountCommand ValidCommand() => new(
        AccountNumber: "1234567890",
        AccountHolder: "علی رضایی",
        CardNumber: "6037991234567890",
        ShebaNumber: "IR120170000000123456789012",
        FirstAmount: 1_000_000m,
        BankId: Guid.NewGuid(),
        BranchId: Guid.NewGuid(),
        AccountTypeId: Guid.NewGuid(),
        AccountCodeId: Guid.NewGuid(),
        CheckFile: null,
        AccountOpeningDate: "13990101")
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
    public void Validate_AllOptionalsNull_Passes()
    {
        // VahedCode is deliberately excluded from this "all optionals null" case: it is no
        // longer a client-optional field — VahedScopeBehavior always assigns a real,
        // non-empty value before this validator runs (see CreateBankAccountCommand.VahedCode
        // XML doc), so NotEmpty() on it is correct even though every other field here is
        // genuinely nullable in Legacy.
        var result = _validator.Validate(ValidCommand() with
        {
            CardNumber = null,
            ShebaNumber = null,
            FirstAmount = null,
            BankId = null,
            BranchId = null,
            AccountTypeId = null,
            AccountCodeId = null,
            AccountOpeningDate = null,
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankAccountCommand.VahedCode));
    }

    [Fact]
    public void Validate_EmptyAccountNumber_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountNumber = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankAccountCommand.AccountNumber));
    }

    [Fact]
    public void Validate_AccountNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountNumber = new string('1', 16) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankAccountCommand.AccountNumber));
    }

    [Fact]
    public void Validate_EmptyAccountHolder_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountHolder = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankAccountCommand.AccountHolder));
    }

    [Fact]
    public void Validate_AccountHolderTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountHolder = new string('a', 81) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankAccountCommand.AccountHolder));
    }

    [Fact]
    public void Validate_CardNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { CardNumber = new string('1', 17) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankAccountCommand.CardNumber));
    }

    [Fact]
    public void Validate_ShebaNumberTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ShebaNumber = new string('1', 51) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankAccountCommand.ShebaNumber));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankAccountCommand.VahedCode));
    }

    [Fact]
    public void Validate_AccountOpeningDateTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { AccountOpeningDate = "139901011" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateBankAccountCommand.AccountOpeningDate));
    }
}
