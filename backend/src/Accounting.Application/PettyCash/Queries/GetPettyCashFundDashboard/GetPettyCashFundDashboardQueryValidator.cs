using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundDashboard;

public sealed class GetPettyCashFundDashboardQueryValidator : AbstractValidator<GetPettyCashFundDashboardQuery>
{
    public GetPettyCashFundDashboardQueryValidator()
    {
        RuleFor(x => x.FundId).NotEqual(Guid.Empty);
    }
}
