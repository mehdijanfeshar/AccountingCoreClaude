using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.UpsertPettyCashFundReviewer;

public sealed class UpsertPettyCashFundReviewerCommandValidator : AbstractValidator<UpsertPettyCashFundReviewerCommand>
{
    public UpsertPettyCashFundReviewerCommandValidator()
    {
        RuleFor(x => x.FundId).NotEmpty();

        RuleFor(x => x.ReviewerUserId)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.ReviewerName).MaximumLength(200);

        RuleFor(x => x.Role).IsInEnum();
    }
}
