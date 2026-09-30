using FluentValidation;

namespace Accounting.Application.Treasury.Commands.CreateBankStatement;

public sealed class CreateBankStatementCommandValidator : AbstractValidator<CreateBankStatementCommand>
{
    private const string LegacyJalaliDatePattern = @"^\d{8}$";

    public CreateBankStatementCommandValidator()
    {
        RuleFor(x => x.BankAccountId).NotEmpty();

        RuleFor(x => x.FromDate)
            .NotEmpty()
            .Matches(LegacyJalaliDatePattern)
            .WithMessage("تاریخ ابتدای دوره باید به شکل YYYYMMDD باشد.");

        RuleFor(x => x.ToDate)
            .NotEmpty()
            .Matches(LegacyJalaliDatePattern)
            .WithMessage("تاریخ انتهای دوره باید به شکل YYYYMMDD باشد.");

        RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.FromDate, x.ToDate) <= 0)
            .WithMessage("تاریخ ابتدای دوره نباید بعد از تاریخ انتها باشد.")
            .When(x => !string.IsNullOrEmpty(x.FromDate) && !string.IsNullOrEmpty(x.ToDate));

        RuleFor(x => x.Description).MaximumLength(1000);

        RuleFor(x => x.Year)
            .NotEmpty()
            .Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم عددی باشد.");

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
