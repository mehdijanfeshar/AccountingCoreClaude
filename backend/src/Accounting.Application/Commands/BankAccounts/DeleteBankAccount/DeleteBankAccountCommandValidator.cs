using FluentValidation;

namespace Accounting.Application.Commands.BankAccounts.DeleteBankAccount;

/// <summary>
/// Surface-level validation only — the single field is the route-bound <c>Id</c>.
/// </summary>
public sealed class DeleteBankAccountCommandValidator : AbstractValidator<DeleteBankAccountCommand>
{
    public DeleteBankAccountCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
