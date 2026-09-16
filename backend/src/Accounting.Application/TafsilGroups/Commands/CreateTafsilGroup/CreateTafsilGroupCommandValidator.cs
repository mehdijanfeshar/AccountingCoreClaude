using FluentValidation;

namespace Accounting.Application.TafsilGroups.Commands.CreateTafsilGroup;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Does NOT pre-check <c>UK_TBTAFSILGROUP</c> uniqueness — that is left
/// to the DB constraint, mapped centrally to 409 by <c>UnitOfWork</c>.
///
/// <c>.IsInEnum()</c> on <see cref="CreateTafsilGroupCommand.PersonType"/> only rejects an
/// out-of-range underlying integer (e.g. <c>(PersonTypes)99</c>) — added in phase 27 batch 1
/// alongside the <c>bool?</c>→enum fix for this column.
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

        RuleFor(x => x.PersonType)
            .IsInEnum()
            .When(x => x.PersonType.HasValue);
    }
}
