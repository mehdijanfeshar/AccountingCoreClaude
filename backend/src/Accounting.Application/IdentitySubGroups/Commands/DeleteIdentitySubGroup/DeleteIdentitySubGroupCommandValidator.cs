using FluentValidation;

namespace Accounting.Application.IdentitySubGroups.Commands.DeleteIdentitySubGroup;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteIdentitySubGroupCommandValidator : AbstractValidator<DeleteIdentitySubGroupCommand>
{
    public DeleteIdentitySubGroupCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
