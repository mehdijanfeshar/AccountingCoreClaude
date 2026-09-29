namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when the same user who approved a درخواست پرداخت's immediately-preceding stage attempts
/// to approve its next stage too — خزانه‌داری، بخش ۴-الف SoD: «یک کاربر دو مرحلهٔ متوالی همان
/// درخواست را تأیید نکند» (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Deliberately checked only
/// on <c>Approve</c>, not <c>Return</c>/<c>Reject</c> — those are not progression through the
/// approval chain. <b>409</b>.
/// </summary>
public sealed class PaymentRequestConsecutiveApproverConflictException : Exception
{
    public PaymentRequestConsecutiveApproverConflictException(Guid paymentRequestId)
        : base($"Caller approved the immediately-preceding stage of payment request {paymentRequestId} and cannot approve the next one too.")
    {
        PaymentRequestId = paymentRequestId;
    }

    public Guid PaymentRequestId { get; }

    public string PublicDetail => "یک کاربر نمی‌تواند دو مرحلهٔ متوالی تأیید یک درخواست پرداخت را انجام دهد.";
}
