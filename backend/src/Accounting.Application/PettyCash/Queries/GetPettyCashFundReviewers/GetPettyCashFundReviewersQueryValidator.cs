using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundReviewers;

public sealed class GetPettyCashFundReviewersQueryValidator : AbstractValidator<GetPettyCashFundReviewersQuery>
{
    public GetPettyCashFundReviewersQueryValidator()
    {
        RuleFor(x => x.FundId).NotEmpty();
    }
}
