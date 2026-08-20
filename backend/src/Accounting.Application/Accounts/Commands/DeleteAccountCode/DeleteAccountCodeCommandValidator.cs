using FluentValidation;

namespace Accounting.Application.Accounts.Commands.DeleteAccountCode;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteAccountCodeCommandValidator : AbstractValidator<DeleteAccountCodeCommand>
{
    public DeleteAccountCodeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
