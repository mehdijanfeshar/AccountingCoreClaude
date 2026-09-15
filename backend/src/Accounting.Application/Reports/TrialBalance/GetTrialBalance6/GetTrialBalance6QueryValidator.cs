using FluentValidation;

namespace Accounting.Application.Reports.TrialBalance.GetTrialBalance6;

/// <summary>
/// Surface-level (syntactic) validation only — identical rule set to
/// <see cref="GetTrialBalance4.GetTrialBalance4QueryValidator"/>; duplicated rather than shared
/// because each Query is a self-contained CQRS slice in this codebase (matching the convention
/// already used throughout <c>Accounting.Application</c>).
/// </summary>
public sealed class GetTrialBalance6QueryValidator : AbstractValidator<GetTrialBalance6Query>
{
    public GetTrialBalance6QueryValidator()
    {
        // See GetTrialBalance4QueryValidator XML doc for why Year is mandatory (fiscal-year
        // baseline for the opening/period window split).
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

        // Required for the Opening ⊆ Total containment to hold — see GetTrialBalance4QueryValidator
        // XML doc for the full algebraic reasoning.
        RuleFor(x => x)
            .Must(x => string.CompareOrdinal(x.FromDate, x.ToDate) <= 0)
            .When(x => x.FromDate is not null && x.ToDate is not null)
            .WithMessage("FromDate must not be later than ToDate.")
            .WithName(nameof(GetTrialBalance6Query.FromDate));

        RuleFor(x => x.Level)
            .IsInEnum();

        RuleFor(x => x.DocLife)
            .InclusiveBetween(0, 4)
            .When(x => x.DocLife.HasValue);
    }
}
