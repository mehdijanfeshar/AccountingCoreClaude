using FluentValidation;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashRefunds;

public sealed class GetPettyCashRefundsQueryValidator : AbstractValidator<GetPettyCashRefundsQuery>
{
    public GetPettyCashRefundsQueryValidator()
    {
        RuleFor(x => x.FundId).NotEqual(Guid.Empty);
    }
}
