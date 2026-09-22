using FluentValidation;

namespace Accounting.Application.IdentityHeads.Commands.DeleteIdentityHead;

/// <summary>
/// ⚠️ Not dead code — see <c>UpdateIdentityHeadCommandValidator</c> for why this is kept despite
/// <c>ValidationBehavior</c> not reaching non-generic <c>IRequest</c> commands today (risk #1-الف).
/// </summary>
public sealed class DeleteIdentityHeadCommandValidator : AbstractValidator<DeleteIdentityHeadCommand>
{
    public DeleteIdentityHeadCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
