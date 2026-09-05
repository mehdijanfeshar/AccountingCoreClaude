using Accounting.Application.Expenses.Commands.DeleteExpense;

namespace Accounting.Application.Tests.Expenses.Commands.DeleteExpense;

public sealed class DeleteExpenseCommandValidatorTests
{
    private readonly DeleteExpenseCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
    {
        var result = _validator.Validate(new DeleteExpenseCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteExpenseCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteExpenseCommand.Id));
    }
}
