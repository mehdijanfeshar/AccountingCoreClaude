using FluentValidation;

namespace Accounting.Application.Treasury.Commands.UpsertTreasurySetting;

public sealed class UpsertTreasurySettingCommandValidator : AbstractValidator<UpsertTreasurySettingCommand>
{
    public UpsertTreasurySettingCommandValidator()
    {
        RuleFor(x => x.CeoApprovalThreshold).GreaterThan(0m);
        RuleFor(x => x.BulkApproveLimit).GreaterThan(0m);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
