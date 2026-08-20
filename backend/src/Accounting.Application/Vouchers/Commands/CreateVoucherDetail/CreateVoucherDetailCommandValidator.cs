using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.CreateVoucherDetail;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints in
/// <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces the rich model"
/// architecture decision, accounting invariants (debit==credit balance, non-negative amounts,
/// mutually-exclusive debit/credit, account-is-leaf, etc.) were deliberately discarded and must
/// NOT be re-created here.
/// </summary>
public sealed class CreateVoucherDetailCommandValidator : AbstractValidator<CreateVoucherDetailCommand>
{
    public CreateVoucherDetailCommandValidator()
    {
        RuleFor(x => x.VoucherHeadId)
            .NotEmpty();

        RuleFor(x => x.Description)
            .MaximumLength(200);

        RuleFor(x => x.VahedCode)
            .MaximumLength(4);

        RuleFor(x => x.Year)
            .MaximumLength(4);
    }
}
