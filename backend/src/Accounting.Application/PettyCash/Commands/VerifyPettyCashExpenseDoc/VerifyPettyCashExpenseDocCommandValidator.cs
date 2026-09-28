using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.VerifyPettyCashExpenseDoc;

public sealed class VerifyPettyCashExpenseDocCommandValidator : AbstractValidator<VerifyPettyCashExpenseDocCommand>
{
    public VerifyPettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
