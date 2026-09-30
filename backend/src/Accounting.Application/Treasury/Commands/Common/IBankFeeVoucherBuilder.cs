using Accounting.Domain.Entity;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Builds (stages, does not save) the two-line temporary GL voucher for resolving an unmatched
/// withdrawal صورت‌حساب line as <see cref="Accounting.Domain.ValueObjects.BankStatementLineResolutionType.BankFeeVoucher"/>
/// («کارمزد بانکی») — خزانه‌داری، بخش ۴-د (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Debit
/// <c>TB_TR_SETTING.BANK_FEE_ACCOUNT_ID</c>, credit the statement's bank حساب معین (with its
/// تفصیلی links) — same two-line shape as <c>TransferVoucherBuilder</c>/<c>ReceiptVoucherBuilder</c>,
/// reusing <see cref="PaymentRequestVoucherBuildResult"/> for the result (see that record's XML
/// doc — genuinely not payment-request-specific).
/// </summary>
public interface IBankFeeVoucherBuilder
{
    Task<PaymentRequestVoucherBuildResult> BuildAndStageAsync(
        TB_TR_BANK_STATEMENT_LINE line,
        Guid bankAccountId,
        string vahedCode,
        CancellationToken cancellationToken = default);
}
