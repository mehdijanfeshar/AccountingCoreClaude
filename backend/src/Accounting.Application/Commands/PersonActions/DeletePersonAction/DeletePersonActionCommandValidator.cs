using FluentValidation;

namespace Accounting.Application.Commands.PersonActions.DeletePersonAction;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeletePersonActionCommandValidator : AbstractValidator<DeletePersonActionCommand>
{
    public DeletePersonActionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
