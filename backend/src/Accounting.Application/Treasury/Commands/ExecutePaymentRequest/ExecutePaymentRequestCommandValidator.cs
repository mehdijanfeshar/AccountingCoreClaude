using FluentValidation;

namespace Accounting.Application.Treasury.Commands.ExecutePaymentRequest;

public sealed class ExecutePaymentRequestCommandValidator : AbstractValidator<ExecutePaymentRequestCommand>
{
    private const string LegacyJalaliDatePattern = @"^\d{8}$";

    // IR + 24 digits — owner decision ۲۰۲۶-۰۹-۲۹: "IR + 24 digits format check", no checksum.
    private const string IbanPattern = @"^IR\d{24}$";

    public ExecutePaymentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.BankReference)
            .NotEmpty()
            .WithMessage("شمارهٔ پیگیری/مرجع بانکی الزامی است.")
            .MaximumLength(100);

        RuleFor(x => x.PaidDate)
            .NotEmpty()
            .Matches(LegacyJalaliDatePattern)
            .WithMessage("تاریخ پرداخت باید به شکل YYYYMMDD باشد.");

        RuleFor(x => x.DestinationIban)
            .Matches(IbanPattern)
            .WithMessage("شمارهٔ شبا باید به شکل IR و ۲۴ رقم باشد.")
            .When(x => !string.IsNullOrEmpty(x.DestinationIban));

        RuleFor(x => x.PaymentMethod).IsInEnum().When(x => x.PaymentMethod.HasValue);
    }
}
