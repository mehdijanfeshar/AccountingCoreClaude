using FluentValidation;

namespace Accounting.Application.Commands.PettyCash.FinalizePettyCashSettlement;

public sealed class FinalizePettyCashSettlementCommandValidator : AbstractValidator<FinalizePettyCashSettlementCommand>
{
    public FinalizePettyCashSettlementCommandValidator()
    {
        RuleFor(x => x.FundId).NotEqual(Guid.Empty);
    }
}
