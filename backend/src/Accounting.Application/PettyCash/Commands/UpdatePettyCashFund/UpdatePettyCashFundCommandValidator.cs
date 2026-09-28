using FluentValidation;

namespace Accounting.Application.PettyCash.Commands.UpdatePettyCashFund;

public sealed class UpdatePettyCashFundCommandValidator : AbstractValidator<UpdatePettyCashFundCommand>
{
    public UpdatePettyCashFundCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

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
