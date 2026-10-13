using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.CountPettyCashSettlement;

public sealed class CountPettyCashSettlementCommandValidator : AbstractValidator<CountPettyCashSettlementCommand>
{
    public CountPettyCashSettlementCommandValidator()
    {
        RuleFor(x => x.FundId).NotEqual(Guid.Empty);
        RuleFor(x => x.CountedBalance).GreaterThanOrEqualTo(0m);
    }
}
