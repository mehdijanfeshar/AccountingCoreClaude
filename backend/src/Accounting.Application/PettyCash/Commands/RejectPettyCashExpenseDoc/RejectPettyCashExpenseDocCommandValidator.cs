using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.RejectPettyCashExpenseDoc;

public sealed class RejectPettyCashExpenseDocCommandValidator : AbstractValidator<RejectPettyCashExpenseDocCommand>
{
    public RejectPettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
