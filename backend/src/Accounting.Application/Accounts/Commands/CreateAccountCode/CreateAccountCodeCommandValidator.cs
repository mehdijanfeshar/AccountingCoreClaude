using FluentValidation;

namespace Accounting.Application.Accounts.Commands.CreateAccountCode;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants (hierarchy rules, required-detail, etc.)
/// were deliberately discarded and must NOT be re-created here.
///
/// <c>.IsInEnum()</c> on the four enum fields below only rejects an out-of-range underlying
/// integer (e.g. <c>(TypeActivity)99</c>) — it deliberately does NOT enforce the reference
/// project's level-dependent range rule (group restricted to <c>TypeActivity</c> 1..3, معین
/// 1..7; see <c>AddGroupCodeValidator.cs:26-29</c> in <c>D:\CentralAccount</c>). That would be a
/// new business invariant, which "Legacy fully replaces the rich model" forbids inventing here —
/// and our own live Legacy data already violates it (3 group-level accounts with
/// <c>TYPEACTIVITY ∈ {4,5,6}</c>, open risk #13 in <c>CLAUDE.md</c>). This omission is
/// deliberate, not an oversight.
/// </summary>
public sealed class CreateAccountCodeCommandValidator : AbstractValidator<CreateAccountCodeCommand>
{
    public CreateAccountCodeCommandValidator()
    {
        RuleFor(x => x.AccCode)
            .NotEmpty()
            .MaximumLength(6);

        RuleFor(x => x.AccCodeName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.MoInforClose)
            .MaximumLength(6);

        // All four stay optional (no .NotNull()) — none of these are currently required
        // columns; making them required would be a separate business decision, out of scope
        // here. IsInEnum() on a nullable enum treats null as valid (confirmed empirically by
        // this validator's own test suite), so no extra null-guard is needed.
        RuleFor(x => x.TypeCode)
            .IsInEnum();

        RuleFor(x => x.TypeActivity)
            .IsInEnum();

        RuleFor(x => x.TypeAccCode)
            .IsInEnum();

        RuleFor(x => x.TypeAction)
            .IsInEnum();
    }
}
