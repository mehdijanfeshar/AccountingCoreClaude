using FluentValidation;

namespace Accounting.Application.IdentitySubGroups.Commands.CreateIdentitySubGroup;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. <see cref="CreateIdentitySubGroupCommand.IdentyGroupsId"/> only gets a
/// <c>NotEqual(Guid.Empty)</c> rule (mirrors <c>CreateAccountExceptionCommandValidator</c>) — the
/// referential existence check itself is left to the DB constraint <c>FK_IDENTYSU_IDENTYGR</c>,
/// mapped centrally to 400 (see the command's XML doc). <c>SubgrpsLen</c> maps to
/// <c>SUBGRPS_LEN</c>, an Oracle <c>NUMBER(2)</c> column (max 99) — narrower than its CLR type
/// (<c>byte</c>, 0-255), so an explicit upper-bound rule is needed here; without it, a value like
/// 150 would pass FluentValidation and only fail at <c>SaveChangesAsync</c> with a raw
/// <c>ORA-01438</c>-driven 500 instead of a clean 400.
/// </summary>
public sealed class CreateIdentitySubGroupCommandValidator : AbstractValidator<CreateIdentitySubGroupCommand>
{
    public CreateIdentitySubGroupCommandValidator()
    {
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
