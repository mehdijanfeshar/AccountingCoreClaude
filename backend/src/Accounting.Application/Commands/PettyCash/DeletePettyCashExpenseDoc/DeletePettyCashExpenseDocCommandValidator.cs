using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.DeletePettyCashExpenseDoc;

public sealed class DeletePettyCashExpenseDocCommandValidator : AbstractValidator<DeletePettyCashExpenseDocCommand>
{
    public DeletePettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
