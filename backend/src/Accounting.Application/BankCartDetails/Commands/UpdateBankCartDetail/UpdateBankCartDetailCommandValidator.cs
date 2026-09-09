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

        // Deliberate second belt, not dead code: by the time this validator runs,
        // VahedScopeBehavior (registered ahead of ValidationBehavior — see
        // DependencyInjection.cs) has already overwritten UpdateBankCartDetailCommand.VahedCode
        // with the server-assigned value, so this rule now validates that value rather than
        // anything the caller supplied. NotEmpty() here is safe even though
        // TB_BANKCARTDETAIL.VAHEDCODE is nullable in Legacy, because the server-assigned value is
        // never actually empty.
        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .MaximumLength(4);
    }
}
