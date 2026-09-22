using FluentValidation;

namespace Accounting.Application.Reports.VoucherReview.GetVoucherReview;

/// <summary>
/// Surface validation. Every length limit here mirrors the view's actual column width (read from
/// live <c>ALL_TAB_COLUMNS</c>): a bound longer than the column could never match a row, so it is
/// a caller mistake rather than an empty result.
///
/// <para>
/// The paired bounds must be ordered when both are present. An inverted range returns nothing,
/// which reads to the user as «سندی یافت نشد» rather than «کران‌ها را جابه‌جا وارد کرده‌اید».
/// </para>
/// </summary>
public sealed class GetVoucherReviewQueryValidator : AbstractValidator<GetVoucherReviewQuery>
{
    public const int MaxPageSize = 200;

    /// <summary>
    /// Chosen as <c>int.MaxValue / MaxPageSize</c> so the repository's
    /// <c>(pageNumber - 1) * pageSize</c> offset can never overflow <see cref="int"/>, matching
    /// <c>GetVoucherHeadsQueryValidator</c>.
    /// </summary>
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    public GetVoucherReviewQueryValidator()
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

        // ATFNO is CHAR(15) on the view.
        RuleFor(x => x.FromAtfNo).MaximumLength(15);
        RuleFor(x => x.ToAtfNo).MaximumLength(15);

        RuleFor(x => x.Description).MaximumLength(250);

        RuleFor(x => x.DocLife)
            .InclusiveBetween(0, 4).WithMessage("وضعیت سند نامعتبر است.")
            .When(x => x.DocLife.HasValue);

        RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.FromDate, x.ToDate) <= 0)
            .WithMessage("«از تاریخ» نباید بعد از «تا تاریخ» باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromDate) && !string.IsNullOrWhiteSpace(x.ToDate));

        RuleFor(x => x)
            .Must(x => VoucherNumberOrdering(x.FromVoucherNo!, x.ToVoucherNo!))
            .WithMessage("«از شماره سند» نباید بزرگ‌تر از «تا شماره سند» باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromVoucherNo) && !string.IsNullOrWhiteSpace(x.ToVoucherNo));
    }

    /// <summary>
    /// Compares voucher numbers numerically, not ordinally. The column is a zero-padded string, so
    /// the repository's ordinal range filter is correct against the database — but the user types
    /// «۹» and «۱۰» unpadded, and ordinally "9" &gt; "10". Validating on the padded form would
    /// reject a range the query would then have handled correctly.
    /// </summary>
    private static bool VoucherNumberOrdering(string from, string to)
        => !long.TryParse(from, out var f) || !long.TryParse(to, out var t) || f <= t;
}
