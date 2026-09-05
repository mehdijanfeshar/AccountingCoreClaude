using FluentValidation;

namespace Accounting.Application.BankAccounts.Commands.UpdateBankAccount;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
/// </summary>
public sealed class UpdateBankAccountCommandValidator : AbstractValidator<UpdateBankAccountCommand>
{
    public UpdateBankAccountCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.AccountNumber)
            .NotEmpty()
            .MaximumLength(15);

        RuleFor(x => x.AccountHolder)
            .NotEmpty()
            .MaximumLength(80);

        RuleFor(x => x.CardNumber)
            .MaximumLength(16);

        RuleFor(x => x.ShebaNumber)
            .MaximumLength(50);

        RuleFor(x => x.VahedCode)
            .MaximumLength(4);

        RuleFor(x => x.AccountOpeningDate)
            .MaximumLength(8);
    }
}
