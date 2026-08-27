using FluentValidation;

namespace Accounting.Application.Rabets.Commands.DeleteRabet;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteRabetCommandValidator : AbstractValidator<DeleteRabetCommand>
{
    public DeleteRabetCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
