using FluentValidation;

namespace Accounting.Application.Treasury.Commands.UpdatePaymentRequest;

public sealed class UpdatePaymentRequestCommandValidator : AbstractValidator<UpdatePaymentRequestCommand>
{
    private const string LegacyJalaliDatePattern = @"^\d{8}$";

    public UpdatePaymentRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.BeneficiaryName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.BeneficiaryNationalId).MaximumLength(11);

        RuleFor(x => x.PaymentType).IsInEnum();

        RuleFor(x => x.InvoiceRef).MaximumLength(100);

        RuleFor(x => x.ExpenseAccountId).NotEmpty();

        RuleFor(x => x.AmountBeforeTax).GreaterThan(0m);

        RuleFor(x => x.VatPercent)
            .InclusiveBetween(0m, 100m)
            .When(x => x.VatPercent.HasValue);

        RuleFor(x => x.VatAmount)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.VatAmount.HasValue);

        RuleFor(x => x.InsuranceDeductionPercent)
            .InclusiveBetween(0m, 100m)
            .When(x => x.InsuranceDeductionPercent.HasValue);

        RuleFor(x => x.InsuranceDeductionAmount)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.InsuranceDeductionAmount.HasValue);

        RuleFor(x => x.DueDate)
            .NotEmpty()
            .Matches(LegacyJalaliDatePattern)
            .WithMessage("تاریخ سررسید باید به شکل YYYYMMDD باشد.");

        RuleFor(x => x.PaymentAccountId).NotEmpty();

        RuleFor(x => x.PaymentMethod).IsInEnum().When(x => x.PaymentMethod.HasValue);

        RuleFor(x => x.Description).MaximumLength(1000);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
