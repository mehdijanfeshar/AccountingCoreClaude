using FluentValidation;

namespace Accounting.Application.Receipts.Commands.UpdateReceipt;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model" architecture
/// decision, accounting invariants must NOT be re-created here.
/// </summary>
public sealed class UpdateReceiptCommandValidator : AbstractValidator<UpdateReceiptCommand>
{
    public UpdateReceiptCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

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
