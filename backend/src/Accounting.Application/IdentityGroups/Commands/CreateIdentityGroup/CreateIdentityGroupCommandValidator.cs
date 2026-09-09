using FluentValidation;

namespace Accounting.Application.IdentityGroups.Commands.CreateIdentityGroup;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. <see cref="CreateIdentityGroupCommand.TafsiliId"/> carries no rule —
/// it is an optional FK protected by the real DB constraint <c>FK_IDENTITY_TAFSILI</c>, mapped
/// centrally to 400 (see the command's XML doc).
///
/// The <c>RuleFor(x => x.VahedCode)</c> below is a deliberate second belt, not dead code: by the
/// time this validator runs, <c>VahedScopeBehavior</c> (registered ahead of
/// <c>ValidationBehavior</c> — see <c>DependencyInjection.cs</c>) has already overwritten
/// <see cref="CreateIdentityGroupCommand.VahedCode"/> with the server-assigned value, so this
/// rule now validates that value rather than anything the caller supplied.
/// </summary>
public sealed class CreateIdentityGroupCommandValidator : AbstractValidator<CreateIdentityGroupCommand>
{
    public CreateIdentityGroupCommandValidator()
    {
        RuleFor(x => x.IdentityGroupsDesc)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.IdentityGroupsCode)
            .MaximumLength(3);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);
    }
}
