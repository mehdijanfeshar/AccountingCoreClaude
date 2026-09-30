using FluentValidation;

namespace Accounting.Application.Treasury.Commands.AddBankStatementLine;

/// <summary>Surface-level validation only. «دقیقاً یکی از برداشت/واریز باید مثبت باشد» به‌جای دو
/// قانون جدا (هرکدام <c>GreaterThanOrEqualTo(0)</c>) این‌جا با یک قانون ترکیبی چک می‌شود.</summary>
public sealed class AddBankStatementLineCommandValidator : AbstractValidator<AddBankStatementLineCommand>
{
    private const string LegacyJalaliDatePattern = @"^\d{8}$";

    public AddBankStatementLineCommandValidator()
    {
        RuleFor(x => x.StatementId).NotEmpty();

        RuleFor(x => x.LineDate)
            .NotEmpty()
            .Matches(LegacyJalaliDatePattern)
            .WithMessage("تاریخ ردیف باید به شکل YYYYMMDD باشد.");

        RuleFor(x => x.BankReference).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);

        RuleFor(x => x.Withdrawal).GreaterThanOrEqualTo(0m);
        RuleFor(x => x.Deposit).GreaterThanOrEqualTo(0m);

        RuleFor(x => x)
            .Must(x => (x.Withdrawal > 0) ^ (x.Deposit > 0))
            .WithMessage("دقیقاً یکی از مبلغ برداشت یا واریز باید بزرگ‌تر از صفر باشد.");

        RuleFor(x => x.VahedCode).NotEmpty().MaximumLength(4);
    }
}
