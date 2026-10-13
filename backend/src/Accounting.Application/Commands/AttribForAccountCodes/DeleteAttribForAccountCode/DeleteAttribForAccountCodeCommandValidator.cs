using FluentValidation;

namespace Accounting.Application.Commands.AttribForAccountCodes.DeleteAttribForAccountCode;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteAttribForAccountCodeCommandValidator : AbstractValidator<DeleteAttribForAccountCodeCommand>
{
    public DeleteAttribForAccountCodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
