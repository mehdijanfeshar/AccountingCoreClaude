using FluentValidation;

namespace Accounting.Application.Commands.ElamHeads.DeleteElamHead;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteElamHeadCommandValidator : AbstractValidator<DeleteElamHeadCommand>
{
    public DeleteElamHeadCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
