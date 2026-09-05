using FluentValidation;

namespace Accounting.Application.BankCartDetails.Commands.CreateBankCartDetail;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants (e.g. Debtor/Creditor balance) must NOT be re-created here. No
/// rule is applied to any of the five optional <see cref="Guid"/>? fields
/// (<see cref="CreateBankCartDetailCommand.ReceipId"/>, <see cref="CreateBankCartDetailCommand.CheckId"/>,
/// <see cref="CreateBankCartDetailCommand.BankId"/>, <see cref="CreateBankCartDetailCommand.BranchId"/>,
/// <see cref="CreateBankCartDetailCommand.CheckIncorrentId"/>) beyond nullability, since they are
/// all genuinely optional.
/// </summary>
public sealed class CreateBankCartDetailCommandValidator : AbstractValidator<CreateBankCartDetailCommand>
{
    public CreateBankCartDetailCommandValidator()
    {
        RuleFor(x => x.AccountNumber)
            .MaximumLength(13);

        RuleFor(x => x.Month)
            .MaximumLength(2);

        RuleFor(x => x.Cheqno)
            .MaximumLength(8);

        RuleFor(x => x.RecivDate)
            .MaximumLength(8);

        RuleFor(x => x.VahedCode)
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .MaximumLength(4);
    }
}
