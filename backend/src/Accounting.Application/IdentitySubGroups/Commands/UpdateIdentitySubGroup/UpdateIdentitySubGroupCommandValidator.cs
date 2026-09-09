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
/// ⚠️ The <c>RuleFor(x => x.VahedCode)</c> below currently does NOT execute at runtime:
/// <c>ValidationBehavior</c>'s <c>where TRequest : IRequest&lt;TResponse&gt;</c> constraint is
/// never satisfied for this void (<c>: IRequest</c>) command in MediatR 14.2.0, so the DI
/// container silently skips this validator for every <c>Update</c>/<c>Delete</c> request
/// project-wide — see <c>docs/open-decisions.md</c> and <c>VahedScopeBehavior.cs</c> (which
/// deliberately avoids the same constraint for this exact reason).
/// <see cref="UpdateIdentitySubGroupCommand.VahedCode"/> is still safe at runtime only because
/// <c>VahedScopeBehavior</c> unconditionally overwrites it before the handler runs, not because
/// of this rule.
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
    }
}
