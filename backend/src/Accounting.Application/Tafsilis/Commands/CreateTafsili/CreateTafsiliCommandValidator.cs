using FluentValidation;

namespace Accounting.Application.Tafsilis.Commands.CreateTafsili;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Does NOT pre-check <c>UK_TASILI</c> uniqueness — that is left to the
/// DB constraint, mapped centrally to 409 by <c>UnitOfWork</c>.
///
/// <c>.IsInEnum()</c> on <see cref="CreateTafsiliCommand.IsActive"/>,
/// <see cref="CreateTafsiliCommand.PersonType"/>, <see cref="CreateTafsiliCommand.Owner"/> and
/// <see cref="CreateTafsiliCommand.VahedType"/> only rejects an out-of-range underlying integer
/// (e.g. <c>(TafsiliActiveState)99</c>) — added in phase 27 batch 1 alongside the
/// <c>bool?</c>→enum fix for these four columns, mirroring <c>CreateAccountCodeCommandValidator</c>
/// (phase 25).
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

        RuleFor(x => x.IsActive)
            .IsInEnum()
            .When(x => x.IsActive.HasValue);

        RuleFor(x => x.PersonType)
            .IsInEnum()
            .When(x => x.PersonType.HasValue);

        RuleFor(x => x.Owner)
            .IsInEnum()
            .When(x => x.Owner.HasValue);

        RuleFor(x => x.VahedType)
            .IsInEnum()
            .When(x => x.VahedType.HasValue);

        RuleFor(x => x.TafsilGroupLinkVahedType)
            .IsInEnum()
            .When(x => x.TafsilGroupLinkVahedType.HasValue);
    }
}
