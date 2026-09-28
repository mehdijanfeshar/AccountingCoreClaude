using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.FinalizePettyCashSettlement;

public sealed class FinalizePettyCashSettlementCommandValidator : AbstractValidator<FinalizePettyCashSettlementCommand>
{
    public FinalizePettyCashSettlementCommandValidator()
    {
        RuleFor(x => x.FundId).NotEqual(Guid.Empty);
    }
}
