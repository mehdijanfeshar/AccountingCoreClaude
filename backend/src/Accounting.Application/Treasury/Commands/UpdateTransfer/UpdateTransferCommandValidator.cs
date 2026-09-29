using FluentValidation;

namespace Accounting.Application.Treasury.Commands.UpdateTransfer;

public sealed class UpdateTransferCommandValidator : AbstractValidator<UpdateTransferCommand>
{
    private const string LegacyJalaliDatePattern = @"^\d{8}$";

    public UpdateTransferCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.SourceBankAccountId).NotEmpty();

        RuleFor(x => x.DestBankAccountId)
            .NotEmpty()
            .NotEqual(x => x.SourceBankAccountId)
            .WithMessage("حساب بانکی مبدأ و مقصد باید متفاوت باشند.");

        RuleFor(x => x.Amount).GreaterThan(0m);

        RuleFor(x => x.TransferDate)
            .NotEmpty()
            .Matches(LegacyJalaliDatePattern)
            .WithMessage("تاریخ انتقال باید به شکل YYYYMMDD باشد.");

        RuleFor(x => x.TransferMethod).IsInEnum();

        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
