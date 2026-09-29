namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when a caller who is not a درخواست پرداخت's own creator (<c>ADDUSERID</c>) attempts to
/// update/delete/submit it — خزانه‌داری، بخش ۴-الف: «ویرایش/حذف فقط Draft/Returned و فقط
/// ثبت‌کننده» (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Straight permissions gap, same shape as
/// <c>PettyCashNotCustodianException</c>.
/// </summary>
public sealed class PaymentRequestNotCreatorException : Exception
{
    public PaymentRequestNotCreatorException(Guid paymentRequestId)
        : base($"Caller is not the creator of payment request {paymentRequestId}.")
    {
        PaymentRequestId = paymentRequestId;
    }

    public Guid PaymentRequestId { get; }

    public string PublicDetail => "فقط ثبت‌کنندهٔ درخواست پرداخت می‌تواند این اقدام را انجام دهد.";
}
