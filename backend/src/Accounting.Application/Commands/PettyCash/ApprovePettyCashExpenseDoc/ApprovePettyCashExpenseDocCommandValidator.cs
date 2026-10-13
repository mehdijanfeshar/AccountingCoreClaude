using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.ApprovePettyCashExpenseDoc;

public sealed class ApprovePettyCashExpenseDocCommandValidator : AbstractValidator<ApprovePettyCashExpenseDocCommand>
{
    public ApprovePettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Note).MaximumLength(1000);
    }
}
