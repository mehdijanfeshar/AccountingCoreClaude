using Accounting.Domain.Entity;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Builds and stages (never saves) سند «شناسایی بدهی» (voucher 1) — خزانه‌داری، بخش ۴-ب (owner
/// decision ۲۰۲۶-۰۹-۲۹ #۲، <c>docs/tankhah-khazaneh-module.md</c> §۱۰). Issued exactly once per
/// درخواست پرداخت, at the moment it transitions to
/// <see cref="Accounting.Domain.ValueObjects.PaymentRequestState.ReadyForExecution"/> — called from
/// <see cref="PaymentRequestApprovalService.ApproveAsync"/>, so it runs inside the SAME
/// <see cref="Accounting.Application.Common.Interfaces.IUnitOfWork.SaveChangesAsync"/> call as the
/// state transition: if the voucher cannot be built, the transition itself is never persisted.
///
/// Built entirely on the same shared write-side components every other voucher path in this
/// project uses (<c>IVoucherHeadRepository</c>/<c>IVoucherDetailRepository</c>/
/// <c>IVoucherTafsiliLevelGuard</c>) — same shape as <c>PettyCashSettlementVoucherBuilder</c>.
/// </summary>
public interface IPaymentRequestLiabilityVoucherBuilder
{
    /// <exception cref="Accounting.Application.Common.Exceptions.PaymentRequestTreasurySettingAccountMissingException">
    /// A needed <c>TB_TR_SETTING</c> account column is <see langword="null"/>.</exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.PaymentRequestPayablesBeneficiaryRequiredException" />
    /// <exception cref="Accounting.Application.Common.Exceptions.PaymentRequestVoucherAccountConfigException" />
    /// <exception cref="Accounting.Application.Common.Exceptions.PaymentRequestVoucherTafsiliMissingException" />
    /// <exception cref="Accounting.Application.Common.Exceptions.PaymentRequestVoucherUnbalancedException">Defensive.</exception>
    Task<PaymentRequestVoucherBuildResult> BuildAndStageAsync(
        TB_TR_PAYMENT_REQUEST paymentRequest,
        string vahedCode,
        CancellationToken cancellationToken = default);
}
