using FluentValidation;

namespace Accounting.Application.IdentityGroups.Commands.DeleteIdentityGroup;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteIdentityGroupCommandValidator : AbstractValidator<DeleteIdentityGroupCommand>
{
    public DeleteIdentityGroupCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
