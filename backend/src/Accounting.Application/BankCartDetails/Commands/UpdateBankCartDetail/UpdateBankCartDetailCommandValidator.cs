using FluentValidation;

namespace Accounting.Application.BankCartDetails.Commands.UpdateBankCartDetail;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
/// </summary>
public sealed class UpdateBankCartDetailCommandValidator : AbstractValidator<UpdateBankCartDetailCommand>
{
    public UpdateBankCartDetailCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

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
