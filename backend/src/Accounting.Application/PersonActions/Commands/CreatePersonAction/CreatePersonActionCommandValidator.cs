using FluentValidation;

namespace Accounting.Application.PersonActions.Commands.CreatePersonAction;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Does NOT pre-check <c>UK_PERSON_ACTION</c> uniqueness — that is left
/// to the DB constraint, mapped centrally to 409 by <c>UnitOfWork</c>.
/// </summary>
public sealed class CreatePersonActionCommandValidator : AbstractValidator<CreatePersonActionCommand>
{
    public CreatePersonActionCommandValidator()
    {
        RuleFor(x => x.UserName)
            .MaximumLength(30);

        RuleFor(x => x.UserId)
            .NotEmpty()
            .MaximumLength(10);

        RuleFor(x => x.FromDate)
            .MaximumLength(8);

        RuleFor(x => x.ToDate)
            .MaximumLength(8);

        RuleFor(x => x.VahedCode)
            .MaximumLength(4);
    }
}
