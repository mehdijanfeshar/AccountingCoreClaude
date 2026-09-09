using Accounting.Application.Expenses.Commands.UpdateExpense;

namespace Accounting.Application.Tests.Expenses.Commands.UpdateExpense;

public sealed class UpdateExpenseCommandValidatorTests
{
    private readonly UpdateExpenseCommandValidator _validator = new();

    private static UpdateExpenseCommand ValidCommand(Guid? id = null) => new(
        Id: id ?? Guid.NewGuid(),
        ExpenseCode: "02",
        ExpenseName: "هزینه به‌روزشده",
        Description: "توضیحات جدید",
        DefaultAmount: 7500m,
        ExpenseGroupId: Guid.NewGuid(),
        AccountCodeId: Guid.NewGuid())
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateExpenseCommand.Id));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateExpenseCommand.ExpenseCode));
    }

    [Fact]
    public void Validate_ExpenseCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ExpenseCode = "012" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateExpenseCommand.ExpenseCode));
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateExpenseCommand.ExpenseName));
    }

    [Fact]
    public void Validate_ExpenseNameTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { ExpenseName = new string('a', 201) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateExpenseCommand.ExpenseName));
    }

    [Fact]
    public void Validate_DescriptionTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { Description = new string('a', 101) });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateExpenseCommand.Description));
    }

    [Fact]
    public void Validate_VahedCodeTooLong_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = "00002" });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateExpenseCommand.VahedCode));
    }

    [Fact]
    public void Validate_EmptyVahedCode_Fails()
    {
        var result = _validator.Validate(ValidCommand() with { VahedCode = string.Empty });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateExpenseCommand.VahedCode));
    }
}
