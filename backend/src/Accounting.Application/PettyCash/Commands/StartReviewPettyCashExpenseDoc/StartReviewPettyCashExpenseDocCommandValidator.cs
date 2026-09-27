using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.StartReviewPettyCashExpenseDoc;

public sealed class StartReviewPettyCashExpenseDocCommandValidator : AbstractValidator<StartReviewPettyCashExpenseDocCommand>
{
    public StartReviewPettyCashExpenseDocCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
