using FluentValidation;

namespace Accounting.Application.Reports.MatrixReport.GetMatrixReport;

/// <summary>
/// Surface validation. The date/voucher-number bounds are optional individually but must be
/// ordered when both are present — an inverted range silently returns nothing, which reads to the
/// user as "no data" rather than "you typed the bounds backwards".
///
/// <para>
/// The scope rules are the load-bearing ones. A path that repeats a level, runs deeper than the
/// level being listed, or is out of order is not a narrower report — it is a contradiction, and
/// answering it with an empty table would look identical to «داده‌ای نیست».
/// </para>
/// </summary>
public sealed class GetMatrixReportQueryValidator : AbstractValidator<GetMatrixReportQuery>
{
    /// <summary>
    /// Longest code any level can carry: <c>TAFSILICODE1..7</c> are VARCHAR2(15) on the view, and
    /// the coding columns are shorter still.
    /// </summary>
    public const int MaxCodeLength = 15;

    public GetMatrixReportQueryValidator()
    {
        RuleFor(x => x.Year)
            .NotEmpty().WithMessage("سال مالی الزامی است.")
            .Matches("^[0-9]{4}$").WithMessage("سال مالی باید ۴ رقم عددی باشد.");

        RuleFor(x => x.Level).IsInEnum();

        RuleForEach(x => x.Scope!).ChildRules(step =>
        {
            step.RuleFor(s => s.Level).IsInEnum().WithMessage("سطح مسیر نامعتبر است.");
            step.RuleFor(s => s.Code)
                .NotEmpty().WithMessage("کد هر مرحله از مسیر الزامی است.")
                .MaximumLength(MaxCodeLength).WithMessage($"کد نباید بیش از {MaxCodeLength} کاراکتر باشد.");
        }).When(x => x.Scope is { Count: > 0 });

        RuleFor(x => x)
            .Must(x => x.Scope!.All(s => s.Level < x.Level))
            .WithMessage("هر مرحله از مسیر باید سطحی بالاتر از سطح گزارش باشد.")
            .When(x => x.Scope is { Count: > 0 });

        RuleFor(x => x)
            .Must(x => x.Scope!.Select(s => s.Level).Distinct().Count() == x.Scope!.Count)
            .WithMessage("هر سطح فقط یک بار می‌تواند در مسیر بیاید.")
            .When(x => x.Scope is { Count: > 0 });

        RuleFor(x => x.FromDate)
            .Matches("^[0-9]{8}$").WithMessage("تاریخ باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromDate));

        RuleFor(x => x.ToDate)
            .Matches("^[0-9]{8}$").WithMessage("تاریخ باید به شکل YYYYMMDD باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.ToDate));

        RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.FromDate, x.ToDate) <= 0)
            .WithMessage("«از تاریخ» نباید بعد از «تا تاریخ» باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromDate) && !string.IsNullOrWhiteSpace(x.ToDate));

        RuleFor(x => x.FromVoucherNo)
            .Matches("^[0-9]{1,6}$").WithMessage("شمارهٔ سند باید حداکثر ۶ رقم عددی باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.FromVoucherNo));

        RuleFor(x => x.ToVoucherNo)
            .Matches("^[0-9]{1,6}$").WithMessage("شمارهٔ سند باید حداکثر ۶ رقم عددی باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.ToVoucherNo));

        RuleFor(x => x.DocLife)
            .InclusiveBetween(0, 4).WithMessage("وضعیت سند نامعتبر است.")
            .When(x => x.DocLife.HasValue);
    }
}
