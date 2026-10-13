using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.StartReviewPettyCashExpenseDoc;

public sealed class StartReviewPettyCashExpenseDocCommandValidator : AbstractValidator<StartReviewPettyCashExpenseDocCommand>
{
    public StartReviewPettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
