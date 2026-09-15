using FluentValidation;

namespace Accounting.Application.AccountExceptions.Commands.DeleteAccountException;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteAccountExceptionCommandValidator : AbstractValidator<DeleteAccountExceptionCommand>
{
    public DeleteAccountExceptionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
