using FluentValidation;

namespace Accounting.Application.Tafsilis.Commands.UpdateTafsili;

/// <summary>
/// Surface-level validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
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
    }
}
