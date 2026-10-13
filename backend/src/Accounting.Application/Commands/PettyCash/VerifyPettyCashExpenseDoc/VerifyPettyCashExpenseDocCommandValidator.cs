using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.VerifyPettyCashExpenseDoc;

public sealed class VerifyPettyCashExpenseDocCommandValidator : AbstractValidator<VerifyPettyCashExpenseDocCommand>
{
    public VerifyPettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
