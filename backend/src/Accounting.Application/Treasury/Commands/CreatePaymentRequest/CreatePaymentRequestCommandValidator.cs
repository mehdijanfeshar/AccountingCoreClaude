using FluentValidation;

namespace Accounting.Application.Treasury.Commands.CreatePaymentRequest;

/// <summary>Surface-level (syntactic) validation only — Submit-only business rules (settings
/// existence, due date vs today, invoice-approved consistency, duplicate) live in
/// <c>IPaymentRequestSubmitRuleChecker</c>, not here.</summary>
public sealed class CreatePaymentRequestCommandValidator : AbstractValidator<CreatePaymentRequestCommand>
{
    private const string LegacyJalaliDatePattern = @"^\d{8}$";

    public CreatePaymentRequestCommandValidator()
    {
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

        RuleFor(x => x.Year)
            .NotEmpty()
            .Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم عددی باشد.");

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
