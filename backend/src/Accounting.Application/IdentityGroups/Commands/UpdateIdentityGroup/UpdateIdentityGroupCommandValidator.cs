using FluentValidation;

namespace Accounting.Application.IdentityGroups.Commands.UpdateIdentityGroup;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
/// </summary>
public sealed class UpdateIdentityGroupCommandValidator : AbstractValidator<UpdateIdentityGroupCommand>
{
    public UpdateIdentityGroupCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

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
