using FluentValidation;

namespace Accounting.Application.Reports.AccountJournal.GetAccountJournal;

/// <summary>
/// Surface validation, mirroring <c>GetVoucherReviewQueryValidator</c>. Length limits come from
/// the view's live column metadata; ordered-range rules exist so an inverted range is reported
/// rather than silently returning nothing.
/// </summary>
public sealed class GetAccountJournalQueryValidator : AbstractValidator<GetAccountJournalQuery>
{
    public const int MaxPageSize = 500;

    /// <summary>
    /// <c>int.MaxValue / MaxPageSize</c>, so the repository's <c>(pageNumber - 1) * pageSize</c>
    /// offset can never overflow <see cref="int"/>.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetAccountJournalQueryValidator()
    {
        RuleFor(x => x.PageNumber).InclusiveBetween(1, MaxPageNumber);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);

        RuleFor(x => x.Year)
            .NotEmpty().WithMessage("سال مالی الزامی است.")
            .Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم عددی باشد.");

        RuleFor(x => x.FromVoucherNo)
            .Matches("^[0-9]{1,6}$").WithMessage("شمارهٔ سند باید حداکثر ۶ رقم عددی باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromVoucherNo));

        RuleFor(x => x.ToVoucherNo)
            .Matches("^[0-9]{1,6}$").WithMessage("شمارهٔ سند باید حداکثر ۶ رقم عددی باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.ToVoucherNo));

        RuleFor(x => x.FromDate)
            .Matches("^[0-9]{8}$").WithMessage("تاریخ باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromDate));

        RuleFor(x => x.ToDate)
            .Matches("^[0-9]{8}$").WithMessage("تاریخ باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.ToDate));

        // ACCOUNTCODE is VARCHAR2(6) on the view — the معین code.
        RuleFor(x => x.FromAccountCode).MaximumLength(6);
        RuleFor(x => x.ToAccountCode).MaximumLength(6);

        RuleFor(x => x.Description).MaximumLength(250);

        RuleFor(x => x.DocLife)
            .InclusiveBetween(0, 4).WithMessage("وضعیت سند نامعتبر است.")
            .When(x => x.DocLife.HasValue);

        RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.FromDate, x.ToDate) <= 0)
            .WithMessage("«از تاریخ» نباید بعد از «تا تاریخ» باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromDate) && !string.IsNullOrWhiteSpace(x.ToDate));

        RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.FromAccountCode, x.ToAccountCode) <= 0)
            .WithMessage("«از کد معین» نباید بزرگ‌تر از «تا کد معین» باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromAccountCode) && !string.IsNullOrWhiteSpace(x.ToAccountCode));

        RuleFor(x => x)
            .Must(x => VoucherNumberOrdering(x.FromVoucherNo!, x.ToVoucherNo!))
            .WithMessage("«از شماره سند» نباید بزرگ‌تر از «تا شماره سند» باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromVoucherNo) && !string.IsNullOrWhiteSpace(x.ToVoucherNo));
    }

    /// <summary>
    /// Numeric comparison, not ordinal — see the identical helper on
    /// <c>GetVoucherReviewQueryValidator</c> for why the padded form must not be validated here.
    /// </summary>
    private static bool VoucherNumberOrdering(string from, string to)
        => !long.TryParse(from, out var f) || !long.TryParse(to, out var t) || f <= t;
}
