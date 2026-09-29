using FluentValidation;

namespace Accounting.Application.Treasury.Commands.CreateReceipt;

/// <summary>Surface-level (syntactic) validation only — تفصیلی گروه membership lives in
/// <c>IReceiptPayerValidator</c>, duplicate بانک reference lives in
/// <c>ITreasuryReceiptRepository.ExistsDuplicateBankReferenceAsync</c>, not here.</summary>
public sealed class CreateReceiptCommandValidator : AbstractValidator<CreateReceiptCommand>
{
    private const string LegacyJalaliDatePattern = @"^\d{8}$";

    public CreateReceiptCommandValidator()
    {
        RuleFor(x => x.PayerTafsiliId).NotEmpty();

        RuleFor(x => x.Amount).GreaterThan(0m);

        RuleFor(x => x.BankAccountId).NotEmpty();

        RuleFor(x => x.ReceiptMethod).IsInEnum();

        RuleFor(x => x.ReceiptDate)
            .NotEmpty()
            .Matches(LegacyJalaliDatePattern)
            .WithMessage("تاریخ دریافت باید به شکل YYYYMMDD باشد.");

        RuleFor(x => x.BankReference)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.InvoiceRef).MaximumLength(100);

        RuleFor(x => x.Description).MaximumLength(1000);

        RuleFor(x => x.Year)
            .NotEmpty()
            .Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم عددی باشد.");

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
