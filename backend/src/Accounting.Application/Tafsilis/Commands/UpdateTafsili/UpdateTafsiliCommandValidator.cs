using FluentValidation;

namespace Accounting.Application.Tafsilis.Commands.UpdateTafsili;

/// <summary>
/// Surface-level validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
///
/// <c>.IsInEnum()</c> on <see cref="UpdateTafsiliCommand.IsActive"/>,
/// <see cref="UpdateTafsiliCommand.PersonType"/>, <see cref="UpdateTafsiliCommand.Owner"/> and
/// <see cref="UpdateTafsiliCommand.VahedType"/> mirrors <c>CreateTafsiliCommandValidator</c>
/// (phase 27 batch 1).
/// </summary>
public sealed class UpdateTafsiliCommandValidator : AbstractValidator<UpdateTafsiliCommand>
{
    public UpdateTafsiliCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

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
