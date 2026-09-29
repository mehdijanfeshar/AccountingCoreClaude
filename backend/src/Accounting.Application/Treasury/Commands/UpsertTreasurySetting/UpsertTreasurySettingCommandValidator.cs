using FluentValidation;

namespace Accounting.Application.Treasury.Commands.UpsertTreasurySetting;

public sealed class UpsertTreasurySettingCommandValidator : AbstractValidator<UpsertTreasurySettingCommand>
{
    public UpsertTreasurySettingCommandValidator()
    {
        RuleFor(x => x.CeoApprovalThreshold).GreaterThan(0m);
        RuleFor(x => x.BulkApproveLimit).GreaterThan(0m);

        RuleFor(x => x.BeneficiaryTafsilGroupId)
            .NotEqual(Guid.Empty)
            .When(x => x.BeneficiaryTafsilGroupId.HasValue);

        RuleFor(x => x.PayablesAccountId)
            .NotEqual(Guid.Empty)
            .When(x => x.PayablesAccountId.HasValue);

        RuleFor(x => x.VatCreditAccountId)
            .NotEqual(Guid.Empty)
            .When(x => x.VatCreditAccountId.HasValue);

        RuleFor(x => x.InsurancePayableAccountId)
            .NotEqual(Guid.Empty)
            .When(x => x.InsurancePayableAccountId.HasValue);

        RuleFor(x => x.ReceivablesAccountId)
            .NotEqual(Guid.Empty)
            .When(x => x.ReceivablesAccountId.HasValue);

        RuleFor(x => x.CustomerTafsilGroupId)
            .NotEqual(Guid.Empty)
            .When(x => x.CustomerTafsilGroupId.HasValue);

        RuleFor(x => x.DailyTransferLimit)
            .GreaterThan(0m)
            .When(x => x.DailyTransferLimit.HasValue);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
