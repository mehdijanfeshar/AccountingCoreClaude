using FluentValidation;

namespace Accounting.Application.TafsilGroups.Commands.CreateTafsilGroup;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Does NOT pre-check <c>UK_TBTAFSILGROUP</c> uniqueness — that is left
/// to the DB constraint, mapped centrally to 409 by <c>UnitOfWork</c>.
/// </summary>
public sealed class CreateTafsilGroupCommandValidator : AbstractValidator<CreateTafsilGroupCommand>
{
    public CreateTafsilGroupCommandValidator()
    {
        RuleFor(x => x.TafsilGroupCode)
            .NotEmpty()
            .MaximumLength(3);

        RuleFor(x => x.TafsilGroupName)
            .NotEmpty()
            .MaximumLength(200);
    }
}
