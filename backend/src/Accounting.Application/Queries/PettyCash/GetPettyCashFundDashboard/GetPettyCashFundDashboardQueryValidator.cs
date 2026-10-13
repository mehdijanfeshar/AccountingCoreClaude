using FluentValidation;

namespace Accounting.Application.Queries.PettyCash.GetPettyCashFundDashboard;

public sealed class GetPettyCashFundDashboardQueryValidator : AbstractValidator<GetPettyCashFundDashboardQuery>
{
    public GetPettyCashFundDashboardQueryValidator()
    {
        RuleFor(x => x.FundId).NotEqual(Guid.Empty);
    }
}
