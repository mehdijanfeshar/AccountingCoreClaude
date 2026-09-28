using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.CountPettyCashSettlement;

public sealed class CountPettyCashSettlementCommandValidator : AbstractValidator<CountPettyCashSettlementCommand>
{
    public CountPettyCashSettlementCommandValidator()
    {
        RuleFor(x => x.FundId).NotEqual(Guid.Empty);
        RuleFor(x => x.CountedBalance).GreaterThanOrEqualTo(0m);
    }
}
