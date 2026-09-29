namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>PaymentRequestApprovalService.ApproveAsync</c> when called in bulk-approve mode
/// for a request whose <c>NET_PAYABLE_AMOUNT</c> exceeds the unit's <c>TB_TR_SETTING.BULK_APPROVE_LIMIT</c>
/// — خزانه‌داری، بخش ۴-الف (<c>docs/tankhah-khazaneh-module.md</c> §۱۰): تأیید گروهی فقط برای
/// درخواست‌های زیر سقف. Never thrown by the single-request Approve endpoint. <b>409</b> inside a
/// bulk request (folded into <c>PaymentRequestBulkApproveConflictException.Failures</c> as
/// <c>"over-bulk-limit"</c>).
/// </summary>
public sealed class PaymentRequestBulkLimitExceededException : Exception
{
    public PaymentRequestBulkLimitExceededException(Guid paymentRequestId, decimal netPayableAmount, decimal bulkApproveLimit)
        : base($"Payment request {paymentRequestId}'s net payable amount {netPayableAmount} exceeds the bulk-approve limit {bulkApproveLimit}.")
    {
        PaymentRequestId = paymentRequestId;
    }

    public Guid PaymentRequestId { get; }

    public string PublicDetail => "مبلغ این درخواست از سقف تأیید گروهی بیشتر است.";
}
