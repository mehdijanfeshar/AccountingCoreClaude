using FluentValidation;

namespace Accounting.Application.Receipts.Commands.CreateReceipt;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
///
/// The <c>RuleFor(x => x.VahedCode)</c> below is a deliberate second belt, not dead code: by the
/// time this validator runs, <c>VahedScopeBehavior</c> (registered ahead of
/// <c>ValidationBehavior</c> — see <c>DependencyInjection.cs</c>) has already overwritten
/// <see cref="CreateReceiptCommand.VahedCode"/> with the server-assigned value, so this rule now
/// validates that value rather than anything the caller supplied.
/// </summary>
public sealed class CreateReceiptCommandValidator : AbstractValidator<CreateReceiptCommand>
{
    public CreateReceiptCommandValidator()
    {
        RuleFor(x => x.ReceiptDate)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.ReceiptNo)
            .NotEmpty()
            .MaximumLength(8);

        RuleFor(x => x.DateRsid)
            .MaximumLength(8);

        RuleFor(x => x.VahedCode)
            .NotEmpty()
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .NotEmpty()
            .MaximumLength(4);
    }
}
