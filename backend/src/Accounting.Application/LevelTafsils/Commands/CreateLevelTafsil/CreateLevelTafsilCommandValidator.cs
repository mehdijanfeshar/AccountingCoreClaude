using FluentValidation;

namespace Accounting.Application.LevelTafsils.Commands.CreateLevelTafsil;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>.
/// </summary>
public sealed class CreateLevelTafsilCommandValidator : AbstractValidator<CreateLevelTafsilCommand>
{
    public CreateLevelTafsilCommandValidator()
    {
        RuleFor(x => x.LevelCode)
            .NotEmpty()
            .MaximumLength(2);

        RuleFor(x => x.LevelName)
            .NotEmpty()
            .MaximumLength(50);
    }
}
