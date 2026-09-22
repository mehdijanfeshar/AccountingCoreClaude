using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.SortVouchers;

/// <summary>
/// Surface validation for مرتب‌سازی. The bounds are conditional on
/// <see cref="SortVouchersCommand.SortType"/>, which the reference project's validator does not
/// model — it requires <c>DocNoFrom</c> unconditionally and then checks <c>DocDateTo</c> with the
/// شماره سند rules and messages, so a date-range sort could not pass its own validator.
/// </summary>
public sealed class SortVouchersCommandValidator : AbstractValidator<SortVouchersCommand>
{
    public SortVouchersCommandValidator()
    {
        RuleFor(x => x.SortType).IsInEnum();

        RuleFor(x => x.Year)
            .NotEmpty()
            .Length(4)
            .Matches("^[0-9]{4}$")
            .WithMessage("سال مالی باید ۴ رقم عددی باشد.");

        When(x => x.SortType == VoucherSortType.DocNum, () =>
        {
            RuleFor(x => x.DocNumFrom)
                .NotEmpty().WithMessage("شمارهٔ سند «از» الزامی است.")
                .Matches("^[0-9]{1,6}$").WithMessage("شمارهٔ سند باید حداکثر ۶ رقم عددی باشد.");

            RuleFor(x => x.DocNumTo)
                .NotEmpty().WithMessage("شمارهٔ سند «تا» الزامی است.")
                .Matches("^[0-9]{1,6}$").WithMessage("شمارهٔ سند باید حداکثر ۶ رقم عددی باشد.");

            RuleFor(x => x)
                .Must(x => ParseOrNull(x.DocNumFrom) <= ParseOrNull(x.DocNumTo))
                .WithMessage("شمارهٔ سند «از» نباید بزرگ‌تر از «تا» باشد.")
                .When(x => !string.IsNullOrWhiteSpace(x.DocNumFrom) && !string.IsNullOrWhiteSpace(x.DocNumTo));
        });

        When(x => x.SortType == VoucherSortType.DocDate, () =>
        {
            RuleFor(x => x.DateDocFrom)
                .NotEmpty().WithMessage("تاریخ سند «از» الزامی است.")
                .Matches("^[0-9]{8}$").WithMessage("تاریخ سند باید به شکل YYYYMMDD باشد.");

            RuleFor(x => x.DateDocTo)
                .NotEmpty().WithMessage("تاریخ سند «تا» الزامی است.")
                .Matches("^[0-9]{8}$").WithMessage("تاریخ سند باید به شکل YYYYMMDD باشد.");

            RuleFor(x => x)
                .Must(x => string.CompareOrdinal(x.DateDocFrom, x.DateDocTo) <= 0)
                .WithMessage("تاریخ سند «از» نباید بعد از «تا» باشد.")
                .When(x => !string.IsNullOrWhiteSpace(x.DateDocFrom) && !string.IsNullOrWhiteSpace(x.DateDocTo));
        });
    }

    private static int ParseOrNull(string? value)
        => int.TryParse(value, out var parsed) ? parsed : 0;
}
