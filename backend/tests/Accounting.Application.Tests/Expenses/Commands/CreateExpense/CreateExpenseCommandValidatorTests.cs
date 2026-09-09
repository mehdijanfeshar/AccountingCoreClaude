using Accounting.Application.Expenses.Commands.CreateExpense;

namespace Accounting.Application.Tests.Expenses.Commands.CreateExpense;

public sealed class CreateExpenseCommandValidatorTests
{
    private readonly CreateExpenseCommandValidator _validator = new();

    private static CreateExpenseCommand ValidCommand() => new(
        ExpenseCode: "01",
        ExpenseName: "هزینه اداری",
        Description: "توضیحات نمونه",
        DefaultAmount: 5000m,
        ExpenseGroupId: Guid.NewGuid(),
        AccountCodeId: Guid.NewGuid())
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
    public void Validate_NullOptionalFields_Passes()
    {
        var command = ValidCommand() with
        {
            Description = null,
            DefaultAmount = null,
            ExpenseGroupId = null,
            AccountCodeId = null,
        };

        var result = _validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyExpenseCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ExpenseCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateExpenseCommand.ExpenseCode));
    }

    [Fact]
    public void Validate_ExpenseCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ExpenseCode = "012" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateExpenseCommand.ExpenseCode));
    }

    [Fact]
    public void Validate_ExpenseCodeAtMaxLength_Passes()
    {
        var result = _validator.Validate(ValidCommand() with { ExpenseCode = "99" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyExpenseName_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ExpenseName = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateExpenseCommand.ExpenseName));
    }

    [Fact]
    public void Validate_ExpenseNameTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ExpenseName = new string('a', 201) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateExpenseCommand.ExpenseName));
    }

    [Fact]
    public void Validate_DescriptionTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Description = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateExpenseCommand.Description));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00001" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateExpenseCommand.VahedCode));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateExpenseCommand.VahedCode));
    }
}
