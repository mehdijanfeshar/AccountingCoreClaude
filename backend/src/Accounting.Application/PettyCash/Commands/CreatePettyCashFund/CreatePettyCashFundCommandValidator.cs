using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.CreatePettyCashFund;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. The duplicate-code and معین-existence checks are Application-level
/// (async, DB-backed) and therefore live in <c>CreatePettyCashFundCommandHandler</c>, not here —
/// same "rule lives once" reasoning as <c>PettyCashDuplicateExpenseDocException</c>.
/// </summary>
public sealed class CreatePettyCashFundCommandValidator : AbstractValidator<CreatePettyCashFundCommand>
{
    public CreatePettyCashFundCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.CustodianUserId)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.CustodianName)
            .MaximumLength(200);

        RuleFor(x => x.Ceiling)
            .GreaterThan(0);

        RuleFor(x => x.PerDocLimit)
            .GreaterThan(0)
            .LessThanOrEqualTo(x => x.Ceiling)
                .WithMessage("سقف هر سند نمی‌تواند از سقف تنخواه بیشتر باشد.");

        RuleFor(x => x.FinanceManagerApprovalLimit)
            .GreaterThan(0);

        RuleFor(x => x.AlertThresholdPercent)
            .InclusiveBetween(0, 100)
            .When(x => x.AlertThresholdPercent.HasValue);

        RuleFor(x => x.SettlementPeriod)
            .IsInEnum()
            .When(x => x.SettlementPeriod.HasValue);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
