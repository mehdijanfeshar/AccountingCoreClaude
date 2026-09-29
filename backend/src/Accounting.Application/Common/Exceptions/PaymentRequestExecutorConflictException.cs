namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// SoD on <c>execute</c> — خزانه‌داری، بخش ۴-ب: «اجراکننده ≠ ثبت‌کنندهٔ درخواست» (owner decision
/// ۲۰۲۶-۰۹-۲۹). Conflict-of-interest, not a permissions gap — <b>409</b>, same shape as
/// <c>PaymentRequestApproverConflictException</c> (the approval-chain SoD check this deliberately
/// does not reuse, since the wording is specific to «اجرا»).
/// </summary>
public sealed class PaymentRequestExecutorConflictException : Exception
{
    public PaymentRequestExecutorConflictException(Guid paymentRequestId)
        : base($"Caller is the creator of payment request {paymentRequestId} and cannot execute it.")
    {
        PaymentRequestId = paymentRequestId;
    }

    public Guid PaymentRequestId { get; }

    public string PublicDetail => "ثبت‌کنندهٔ درخواست پرداخت نمی‌تواند اجراکنندهٔ آن هم باشد.";
}
