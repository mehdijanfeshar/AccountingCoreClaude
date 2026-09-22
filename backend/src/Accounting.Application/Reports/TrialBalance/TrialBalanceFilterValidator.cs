using Accounting.Application.Common.Search;
using FluentValidation;

namespace Accounting.Application.Reports.TrialBalance;

/// <summary>
/// Validates one <see cref="SearchParam"/> of a trial balance request. Shared by all three report
/// validators, because all three accept the same fields.
///
/// <para>
/// <b>This is where an unknown field name is stopped.</b> The repository refuses to build SQL for a
/// name it cannot map and throws if it ever sees one, but that throw is a last-resort assertion
/// about our own consistency — it would surface as a 500. A caller who mistypes a field deserves a
/// 400 telling them which names exist, and that is this validator's job.
/// </para>
///
/// <para>
/// Note what is <i>not</i> validated: the value's shape. These fields are text (an account code and
/// a name), so any string is a legitimate thing to search for. Only the length is bounded, to keep
/// an unbounded string out of a LIKE pattern.
/// </para>
/// </summary>
public sealed class TrialBalanceFilterValidator : AbstractValidator<SearchParam>
{
    public const int MaxValueLength = 100;

    public TrialBalanceFilterValidator()
    {
        RuleFor(x => x.Property)
            .NotEmpty()
            .Must(TrialBalanceSearchFields.All.Contains)
            .WithMessage(_ =>
                "Property must be one of: " + string.Join(", ", TrialBalanceSearchFields.All) + ".");

        RuleFor(x => x.Operator)
            .IsInEnum();

        RuleFor(x => x.Value)
            .NotEmpty()
            .MaximumLength(MaxValueLength);
    }
}
