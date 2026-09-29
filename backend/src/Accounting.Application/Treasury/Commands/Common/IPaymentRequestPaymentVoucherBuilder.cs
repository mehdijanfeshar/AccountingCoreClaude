using Accounting.Domain.Entity;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Builds and stages (never saves) سند «پرداخت» (voucher 2) — خزانه‌داری، بخش ۴-ب (owner decision
/// ۲۰۲۶-۰۹-۲۹ #۳). Issued once per درخواست پرداخت, at <c>execute</c>
/// (<c>ExecutePaymentRequestCommandHandler</c>) — <c>Dr</c> «حساب بستانکاران» = <c>NET</c>,
/// <c>Cr</c> بانک معین (<c>TB_ACCOUNT.ACCOUNTCODE_ID</c> of the request's <c>PAYMENT_ACCOUNT_ID</c>)
/// = <c>NET</c>. Same shared write-side components as
/// <see cref="IPaymentRequestLiabilityVoucherBuilder"/>.
/// </summary>
public interface IPaymentRequestPaymentVoucherBuilder
{
    /// <param name="paymentRequest">The request being executed.</param>
    /// <param name="paidDate">شمسی <c>YYYYMMDD</c> — the voucher's <c>DATE_DOC</c>.</param>
    /// <param name="vahedCode" />
    /// <exception cref="Accounting.Application.Common.Exceptions.PaymentRequestTreasurySettingAccountMissingException">
    /// <c>TB_TR_SETTING.PAYABLES_ACCOUNT_ID</c> is <see langword="null"/>.</exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.NotFoundException">
    /// The request's <c>PAYMENT_ACCOUNT_ID</c> bank account, or its <c>ACCOUNTCODE_ID</c>, is missing.</exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.PaymentRequestPayablesBeneficiaryRequiredException" />
    /// <exception cref="Accounting.Application.Common.Exceptions.PaymentRequestVoucherAccountConfigException" />
    /// <exception cref="Accounting.Application.Common.Exceptions.PaymentRequestVoucherTafsiliMissingException" />
    /// <exception cref="Accounting.Application.Common.Exceptions.PaymentRequestVoucherUnbalancedException">Defensive.</exception>
    Task<PaymentRequestVoucherBuildResult> BuildAndStageAsync(
        TB_TR_PAYMENT_REQUEST paymentRequest,
        string paidDate,
        string vahedCode,
        CancellationToken cancellationToken = default);
}
