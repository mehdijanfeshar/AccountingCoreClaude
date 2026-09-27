using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.SubmitPettyCashExpenseDoc;

public sealed class SubmitPettyCashExpenseDocCommandValidator : AbstractValidator<SubmitPettyCashExpenseDocCommand>
{
    public SubmitPettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
