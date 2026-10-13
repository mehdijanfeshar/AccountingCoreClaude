using FluentValidation;

namespace Accounting.Application.Queries.PettyCash.GetPettyCashFundReviewers;

public sealed class GetPettyCashFundReviewersQueryValidator : AbstractValidator<GetPettyCashFundReviewersQuery>
{
    public GetPettyCashFundReviewersQueryValidator()
    {
        RuleFor(x => x.FundId).NotEmpty();
    }
}
