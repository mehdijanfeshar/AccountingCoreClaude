using FluentValidation;

namespace Accounting.Application.Commands.Expenses.DeleteExpense;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteExpenseCommandValidator : AbstractValidator<DeleteExpenseCommand>
{
    public DeleteExpenseCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
