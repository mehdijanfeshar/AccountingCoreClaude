using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.UpsertPettyCashFundSetting;

public sealed class UpsertPettyCashFundSettingCommandValidator : AbstractValidator<UpsertPettyCashFundSettingCommand>
{
    public UpsertPettyCashFundSettingCommandValidator()
    {
        RuleFor(x => x.FundId).NotEmpty();

        RuleFor(x => x.CustodianUserId).MaximumLength(10);
        RuleFor(x => x.CustodianName).MaximumLength(200);

        RuleFor(x => x.PerDocLimit)
            .GreaterThanOrEqualTo(0)
            .When(x => x.PerDocLimit.HasValue);

        RuleFor(x => x.AlertThresholdPercent)
            .InclusiveBetween(0, 100)
            .When(x => x.AlertThresholdPercent.HasValue);

        RuleFor(x => x.SettlementPeriod)
            .IsInEnum()
            .When(x => x.SettlementPeriod.HasValue);
    }
}
