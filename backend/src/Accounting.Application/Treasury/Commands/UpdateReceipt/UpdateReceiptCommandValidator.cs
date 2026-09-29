using FluentValidation;

namespace Accounting.Application.Treasury.Commands.UpdateReceipt;

public sealed class UpdateReceiptCommandValidator : AbstractValidator<UpdateReceiptCommand>
{
    private const string LegacyJalaliDatePattern = @"^\d{8}$";

    public UpdateReceiptCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

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

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
