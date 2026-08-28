using FluentValidation;

namespace Accounting.Application.LevelTafsils.Commands.UpdateLevelTafsil;

/// <summary>
/// Surface-level validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
/// </summary>
public sealed class UpdateLevelTafsilCommandValidator : AbstractValidator<UpdateLevelTafsilCommand>
{
    public UpdateLevelTafsilCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.LevelCode)
            .NotEmpty()
            .MaximumLength(2);

        RuleFor(x => x.LevelName)
            .NotEmpty()
            .MaximumLength(50);
    }
}
