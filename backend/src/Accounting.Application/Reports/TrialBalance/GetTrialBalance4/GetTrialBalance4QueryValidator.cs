using FluentValidation;

namespace Accounting.Application.Reports.TrialBalance.GetTrialBalance4;

/// <summary>
/// Surface-level (syntactic) validation only — no business rule is invented here that Legacy does
/// not already imply via its column widths.
/// </summary>
public sealed class GetTrialBalance4QueryValidator : AbstractValidator<GetTrialBalance4Query>
{
    public GetTrialBalance4QueryValidator()
    {
        // Year is mandatory (not merely an optional filter like VahedCode) because it is what
        // makes the fiscal-year-start baseline expressible without real Jalali/Gregorian date
        // math: the opening balance is scoped to the fiscal year, and in this system the
        // prior-year carry-forward enters as an opening voucher INSIDE that year (see
        // CLAUDE.md). Without a Year filter the query would have no well-defined "start of
        // period" to anchor FromDate against across a chart-of-accounts-sized result set.
        RuleFor(x => x.Year)
            .NotEmpty()
            .Length(4).WithMessage("Year must be exactly 4 characters (matches CHAR(4) column).");

        RuleFor(x => x.FromDate)
            .Length(8).WithMessage("FromDate must be exactly 8 characters (Jalali YYYYMMDD).")
            .Matches("^[0-9]{8}$").WithMessage("FromDate must contain only digits.")
            .When(x => x.FromDate is not null);

        RuleFor(x => x.ToDate)
            .Length(8).WithMessage("ToDate must be exactly 8 characters (Jalali YYYYMMDD).")
            .Matches("^[0-9]{8}$").WithMessage("ToDate must contain only digits.")
            .When(x => x.ToDate is not null);

        // Required for the Opening ⊆ Total containment to hold: the opening predicate {O} is
        // "strictly before FromDate" and the total predicate {T} is "up to and including
        // ToDate". If FromDate > ToDate, the opening window would extend past the cumulative
        // window it is supposed to be a subset of, breaking the invariant
        // Total = Opening + Period (Period could become negative-width or the two windows
        // could overlap in ways the query's algebra does not anticipate).
        RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.FromDate, x.ToDate) <= 0)
            .When(x => x.FromDate is not null && x.ToDate is not null)
            .WithMessage("FromDate must not be later than ToDate.")
            .WithName(nameof(GetTrialBalance4Query.FromDate));

        RuleFor(x => x.Level)
            .IsInEnum();

        RuleFor(x => x.DocLife)
            .InclusiveBetween(0, 4)
            .When(x => x.DocLife.HasValue);
    }
}
