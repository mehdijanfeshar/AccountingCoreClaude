namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown on Submit when another non-deleted, non-rejected request in the same unit already
/// carries the same (شناسهٔ ملی ذی‌نفع, شمارهٔ فاکتور) pair — خزانه‌داری، بخش ۴-الف
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Application-level duplicate (no DB UNIQUE
/// constraint backs it — a rejected/soft-deleted request must not block reuse). <b>409</b>.
/// </summary>
public sealed class PaymentRequestDuplicateException : Exception
{
    public PaymentRequestDuplicateException(Guid paymentRequestId, string beneficiaryNationalId, string invoiceRef)
        : base($"Payment request {paymentRequestId} duplicates an existing request with national id {beneficiaryNationalId} and invoice ref {invoiceRef}.")
    {
        PaymentRequestId = paymentRequestId;
    }

    public Guid PaymentRequestId { get; }

    public string PublicDetail => "درخواست پرداختی با همین شناسهٔ ملی ذی‌نفع و شمارهٔ فاکتور از قبل ثبت شده است.";
}
