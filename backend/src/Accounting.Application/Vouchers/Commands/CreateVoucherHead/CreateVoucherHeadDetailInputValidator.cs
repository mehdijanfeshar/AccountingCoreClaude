using Accounting.Application.Vouchers.Commands.Common;
using FluentValidation;

namespace Accounting.Application.Vouchers.Commands.CreateVoucherHead;

/// <summary>
/// Surface-level validation matching the Fluent mapping constraints for <c>TB_VOUCHERSDETAIL</c>,
/// plus the whole-rial amount rule the owner re-instated on 2026-10-07 (risk #4). Balance is
/// checked on the whole voucher by <see cref="VoucherBalanceGuard"/>, not per line. All FK fields
/// are optional (<c>Guid?</c>), so none of them get a <c>NotEmpty</c> rule.
/// </summary>
public sealed class CreateVoucherHeadDetailInputValidator : AbstractValidator<CreateVoucherHeadDetailInput>
{
    public CreateVoucherHeadDetailInputValidator()
    {
        RuleFor(x => x.Debtor).WholeRialAmount("مبلغ بدهکار");
        RuleFor(x => x.Creditor).WholeRialAmount("مبلغ بستانکار");

        RuleFor(x => x.Description)
            .MaximumLength(200);
    }
}
