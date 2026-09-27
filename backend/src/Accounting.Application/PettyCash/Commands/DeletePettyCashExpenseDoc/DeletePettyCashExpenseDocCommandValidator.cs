using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashExpenseDoc;

public sealed class DeletePettyCashExpenseDocCommandValidator : AbstractValidator<DeletePettyCashExpenseDocCommand>
{
    public DeletePettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
