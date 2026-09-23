using FluentValidation;

namespace Accounting.Application.Reports.CrossTab.GetCrossTabReport;

/// <summary>
/// Surface validation.
///
/// <para>
/// The load-bearing rule is that the two axes must differ. Crossing a dimension with itself is not
/// a narrower report — every cell off the diagonal is empty by construction — and answering it
/// with a mostly-blank grid looks identical to «داده‌ای نیست».
/// </para>
///
/// <para>
/// Date bounds are optional individually but must be ordered when both are present: an inverted
/// range silently returns nothing, which again reads as missing data rather than as a typo.
/// </para>
/// </summary>
public sealed class GetCrossTabReportQueryValidator : AbstractValidator<GetCrossTabReportQuery>
{
    /// <summary>
    /// Longest code any dimension can carry: <c>TAFSILICODE1..7</c> are VARCHAR2(15) on the view,
    /// and the coding columns are shorter still.
    /// </summary>
    public const int MaxCodeLength = 15;

    /// <summary>
    /// Ceiling on how many columns a single pivot may return.
    ///
    /// <para>
    /// Unlike the row count, the column count is a rendering problem as much as a data one: each
    /// column costs two sub-columns on screen, so a pivot over a dimension with a few hundred
    /// values produces a grid nobody can read and a payload out of proportion to its usefulness.
    /// The cap is a guard rail, not the intended way to work — the intended way is
    /// <see cref="GetCrossTabReportQuery.ColumnCodeFilter"/>. On live data the widest dimension
    /// (معین) has 60 distinct values, so this leaves generous headroom.
    /// </para>
    /// </summary>
    public const int MaxColumns = 120;

    public GetCrossTabReportQueryValidator()
    {
        RuleFor(x => x.Year)
            .NotEmpty().WithMessage("سال مالی الزامی است.")
            .Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم عددی باشد.");

        RuleFor(x => x.RowDimension).IsInEnum().WithMessage("بُعد سطر نامعتبر است.");
        RuleFor(x => x.ColumnDimension).IsInEnum().WithMessage("بُعد ستون نامعتبر است.");

        RuleFor(x => x)
            .Must(x => x.RowDimension != x.ColumnDimension)
            .WithMessage("بُعد سطر و ستون نمی‌توانند یکی باشند.")
            .WithName(nameof(GetCrossTabReportQuery.ColumnDimension));

        RuleFor(x => x.FromDate)
            .Matches("^[0-9]{8}$").WithMessage("تاریخ باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromDate));

        RuleFor(x => x.ToDate)
            .Matches("^[0-9]{8}$").WithMessage("تاریخ باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.ToDate));

        // Both are zero-padded YYYYMMDD Jalali text, so an ordinal comparison orders them
        // chronologically.
        RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.FromDate, x.ToDate) <= 0)
            .WithMessage("«از تاریخ» نباید بعد از «تا تاریخ» باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromDate) && !string.IsNullOrWhiteSpace(x.ToDate));

        RuleFor(x => x.RowCodeFilter)
            .MaximumLength(MaxCodeLength)
            .WithMessage($"کد نباید بیش از {MaxCodeLength} کاراکتر باشد.");

        RuleFor(x => x.ColumnCodeFilter)
            .MaximumLength(MaxCodeLength)
            .WithMessage($"کد نباید بیش از {MaxCodeLength} کاراکتر باشد.");

        RuleFor(x => x.DocLife)
            .GreaterThan(0).WithMessage("وضعیت سند نامعتبر است.")
            .When(x => x.DocLife is not null);

        RuleFor(x => x.SystemTypeId)
            .NotEqual(Guid.Empty).WithMessage("نوع سند نامعتبر است.")
            .When(x => x.SystemTypeId is not null);
    }
}
