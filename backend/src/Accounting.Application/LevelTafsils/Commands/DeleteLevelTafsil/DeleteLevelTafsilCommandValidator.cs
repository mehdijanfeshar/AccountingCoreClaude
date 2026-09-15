using FluentValidation;

namespace Accounting.Application.LevelTafsils.Commands.DeleteLevelTafsil;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteLevelTafsilCommandValidator : AbstractValidator<DeleteLevelTafsilCommand>
{
    public DeleteLevelTafsilCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
