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
/// ⚠️ None of the rules below (not just <c>IsInEnum()</c> — <c>Id</c>, <c>ParentId</c>,
/// <c>AccCode</c>, <c>AccCodeName</c>, <c>MoInforClose</c> too) currently execute at runtime:
/// <c>ValidationBehavior</c>'s <c>where TRequest : IRequest&lt;TResponse&gt;</c> constraint is
/// never satisfied for this void (<c>: IRequest</c>) command in MediatR 14.2.0, so the DI
/// container silently skips this validator for every <c>Update</c>/<c>Delete</c> request
/// project-wide — see <c>docs/open-decisions.md</c>, open risk #1-الف in <c>CLAUDE.md</c>, and
/// <c>UpdatePreDescribCommandValidator</c> (same precedent). Concretely: an out-of-range integer
/// (e.g. <c>TYPEACTIVITY = 99</c>) sent to <c>POST /api/account-codes/{id}/update</c> reaches
/// Oracle unvalidated today. Fixing <c>ValidationBehavior</c> is explicitly out of scope for this
/// task; this comment only documents the trap for the next person.
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
