using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.ApprovePettyCashExpenseDoc;

public sealed class ApprovePettyCashExpenseDocCommandValidator : AbstractValidator<ApprovePettyCashExpenseDocCommand>
{
    public ApprovePettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
