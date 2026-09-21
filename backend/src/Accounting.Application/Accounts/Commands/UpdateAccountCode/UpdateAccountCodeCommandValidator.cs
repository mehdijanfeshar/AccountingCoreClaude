using FluentValidation;

namespace Accounting.Application.Accounts.Commands.UpdateAccountCode;

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
///
/// ✅ <b>These rules really do run now — fixed in phase 31; they did not before.</b>
/// <c>ValidationBehavior</c> used to declare <c>where TRequest : IRequest&lt;TResponse&gt;</c>,
/// which MediatR 14 never satisfies for a void (<c>: IRequest</c>) command, so the DI container
/// skipped the validator for every <c>Update</c>/<c>Delete</c> request project-wide — silently,
/// from phase 8 to phase 30. The constraint is gone and
/// <c>BehaviorPipelineConstraintTests</c> fails if it ever comes back. Practical consequence:
/// rules here were written and unit-tested but never exercised against real traffic, so a 400
/// that appears for the first time is most likely this validator finally firing, not a new bug.
/// </summary>
public sealed class UpdateAccountCodeCommandValidator : AbstractValidator<UpdateAccountCodeCommand>
{
    public UpdateAccountCodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x)
            .Must(x => x.ParentId != x.Id)
            .WithMessage("ParentId cannot be the row's own Id — this would create a self-referencing cycle in the coding hierarchy.")
            .WithName("ParentId");

        RuleFor(x => x.AccCode)
            .NotEmpty()
            .MaximumLength(6);

        RuleFor(x => x.AccCodeName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.MoInforClose)
            .MaximumLength(6);

        // All four stay optional (no .NotNull()). See class-level remarks: dead code at
        // runtime today because of the ValidationBehavior/IRequest constraint gap.
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
