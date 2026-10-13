using FluentValidation;

namespace Accounting.Application.Commands.Tafsilis.DeleteTafsili;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteTafsiliCommandValidator : AbstractValidator<DeleteTafsiliCommand>
{
    public DeleteTafsiliCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
