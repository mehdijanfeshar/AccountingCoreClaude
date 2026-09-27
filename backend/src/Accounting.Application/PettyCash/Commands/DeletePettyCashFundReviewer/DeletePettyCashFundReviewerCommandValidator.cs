using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.DeletePettyCashFundReviewer;

public sealed class DeletePettyCashFundReviewerCommandValidator : AbstractValidator<DeletePettyCashFundReviewerCommand>
{
    public DeletePettyCashFundReviewerCommandValidator()
    {
        RuleFor(x => x.FundId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}
