using FluentValidation;

namespace Accounting.Application.VahedInfos.Commands.UpdateVahedInfo;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>, plus the self-reference guard on <c>ParentId</c> — see
/// <see cref="UpdateVahedInfoCommand"/> XML doc for why that guard only applies to Update, not
/// Create.
/// </summary>
public sealed class UpdateVahedInfoCommandValidator : AbstractValidator<UpdateVahedInfoCommand>
{
    public UpdateVahedInfoCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x)
            .Must(x => x.ParentId != x.Id)
            .WithMessage("ParentId cannot be the row's own Id — this would create a self-referencing cycle in the org-unit hierarchy.")
            .WithName("ParentId");

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.VahedName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.CityId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.VahedTypeId)
            .NotEqual(Guid.Empty);
    }
}
