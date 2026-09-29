namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a درخواست پرداخت's own creator attempts to approve/return/reject it — خزانه‌داری،
/// بخش ۴-الف SoD: «تأییدکننده/برگشت‌دهنده/ردکننده ≠ ثبت‌کننده» (<c>docs/tankhah-khazaneh-module.md</c>
/// §۱۰). Conflict-of-interest, not a permissions gap — <b>409</b>.
/// </summary>
public sealed class PaymentRequestApproverConflictException : Exception
{
    public PaymentRequestApproverConflictException(Guid paymentRequestId)
        : base($"Caller is the creator of payment request {paymentRequestId} and cannot act on its approval stages.")
    {
        PaymentRequestId = paymentRequestId;
    }

    public Guid PaymentRequestId { get; }

    public string PublicDetail => "ثبت‌کنندهٔ درخواست پرداخت نمی‌تواند تأییدکننده/برگشت‌دهنده/ردکنندهٔ آن هم باشد.";
}
