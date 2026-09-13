using FluentValidation;

namespace Accounting.Application.Tafsilis.Commands.CreateTafsili;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Does NOT pre-check <c>UK_TASILI</c> uniqueness — that is left to the
/// DB constraint, mapped centrally to 409 by <c>UnitOfWork</c>.
/// </summary>
public sealed class CreateTafsiliCommandValidator : AbstractValidator<CreateTafsiliCommand>
{
    public CreateTafsiliCommandValidator()
    {
        RuleFor(x => x.TafsiliCode)
            .NotEmpty()
            .MaximumLength(15);

        RuleFor(x => x.TafsiliName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.TafsilDesc)
            .MaximumLength(200);

        RuleFor(x => x.TafsilGroupIds)
            .NotNull();

        RuleFor(x => x.TafsilGroupLinkVahedType)
            .IsInEnum()
            .When(x => x.TafsilGroupLinkVahedType.HasValue);
    }
}
