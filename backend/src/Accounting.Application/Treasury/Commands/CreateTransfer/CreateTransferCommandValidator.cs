using FluentValidation;

namespace Accounting.Application.Treasury.Commands.CreateTransfer;

/// <summary>Surface-level validation only. «مبدأ/مقصد باید متفاوت باشند» (owner decision ۲۰۲۶-۰۹-۲۹)
/// is a plain <c>NotEqual</c> check here — 400, no domain exception needed. Both bank accounts
/// belonging to the caller's unit is enforced by <c>IBankAccountReadRepository</c>'s own
/// NotFound/403 posture in the handler, not weakened to 400 here (kept consistent with the rest of
/// the project's unit-ownership convention).</summary>
public sealed class CreateTransferCommandValidator : AbstractValidator<CreateTransferCommand>
{
    private const string LegacyJalaliDatePattern = @"^\d{8}$";

    public CreateTransferCommandValidator()
    {
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

        RuleFor(x => x.Year)
            .NotEmpty()
            .Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم عددی باشد.");

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
