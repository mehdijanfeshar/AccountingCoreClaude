using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.CreateVoucherHead;

/// <summary>
/// Surface-level (syntactic) validation only, matching the Fluent mapping constraints for
/// <c>TB_VOUCHERSDETAIL</c> in <c>LegacyDbContext</c>. Per the recorded "Legacy fully replaces
/// the rich model" architecture decision, accounting invariants (debit==credit balance,
/// non-negative amounts, mutually-exclusive debit/credit, etc.) were deliberately discarded and
/// must NOT be re-created here. All FK fields are optional (<c>Guid?</c>), so none of them get a
/// <c>NotEmpty</c> rule — an absent FK is a valid state, matching the entity's own nullability.
/// </summary>
public sealed class CreateVoucherHeadDetailInputValidator : AbstractValidator<CreateVoucherHeadDetailInput>
{
    public CreateVoucherHeadDetailInputValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(200);
    }
}
