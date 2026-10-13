using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.SubmitPettyCashExpenseDoc;

public sealed class SubmitPettyCashExpenseDocCommandValidator : AbstractValidator<SubmitPettyCashExpenseDocCommand>
{
    public SubmitPettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
