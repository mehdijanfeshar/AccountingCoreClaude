using FluentValidation;

namespace Accounting.Application.IdentitySubGroups.Commands.UpdateIdentitySubGroup;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. <c>SubgrpsLen</c> maps to <c>SUBGRPS_LEN</c>, an Oracle
/// <c>NUMBER(2)</c> column (max 99) — narrower than its CLR type (<c>byte</c>, 0-255), so an
/// explicit upper-bound rule is needed here; without it, a value like 150 would pass
/// FluentValidation and only fail at <c>SaveChangesAsync</c> with a raw <c>ORA-01438</c>-driven
/// 500 instead of a clean 400.
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
public sealed class UpdateIdentitySubGroupCommandValidator : AbstractValidator<UpdateIdentitySubGroupCommand>
{
    public UpdateIdentitySubGroupCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.IdentyGroupsId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.SubgrpsDesc)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.IdentySubGroupsCode)
            .MaximumLength(2);

        RuleFor(x => x.SubgrpsLen)
            .LessThanOrEqualTo((byte)99);

        // .IsInEnum() only rejects an out-of-range underlying integer (e.g. (IdentitySubGroupKind)99) —
        // mirrors CreateIdentitySubGroupCommandValidator.
        RuleFor(x => x.Fixed)
            .IsInEnum();

        RuleFor(x => x.SubgrpsType)
            .IsInEnum()
            .When(x => x.SubgrpsType.HasValue);
    }
}
