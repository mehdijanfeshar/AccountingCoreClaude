using FluentValidation;

namespace Accounting.Application.IdentityGroups.Commands.CreateIdentityGroup;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. <see cref="CreateIdentityGroupCommand.TafsiliId"/> carries no rule —
/// it is an optional FK protected by the real DB constraint <c>FK_IDENTITY_TAFSILI</c>, mapped
/// centrally to 400 (see the command's XML doc).
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
