using FluentValidation;

namespace Accounting.Application.BankAccounts.Commands.CreateBankAccount;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here. <see cref="CreateBankAccountCommand.BankId"/>,
/// <see cref="CreateBankAccountCommand.BranchId"/>, <see cref="CreateBankAccountCommand.AccountTypeId"/>
/// and <see cref="CreateBankAccountCommand.AccountCodeId"/> carry no rule beyond nullability since
/// all four are genuinely optional; <see cref="CreateBankAccountCommand.CheckFile"/> carries no
/// size rule — see the command XML doc.
/// </summary>
public sealed class CreateBankAccountCommandValidator : AbstractValidator<CreateBankAccountCommand>
{
    public CreateBankAccountCommandValidator()
    {
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
