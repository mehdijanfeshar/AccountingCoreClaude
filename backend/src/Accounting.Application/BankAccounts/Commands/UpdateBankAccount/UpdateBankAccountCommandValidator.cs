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

        // Deliberate second belt, not dead code: by the time this validator runs,
        // VahedScopeBehavior (registered ahead of ValidationBehavior — see
        // DependencyInjection.cs) has already overwritten UpdateBankAccountCommand.VahedCode with
        // the server-assigned value, so this rule now validates that value rather than anything
        // the caller supplied. NotEmpty() here is safe even though TB_ACCOUNT.VAHEDCODE is
        // nullable in Legacy, because the server-assigned value is never actually empty.
        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.AccountOpeningDate)
            .MaximumLength(8);
    }
}
