namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown on Submit when <c>INVOICE_REF</c> is set but <c>INVOICE_APPROVED</c> is still
/// <see langword="false"/> — خزانه‌داری، بخش ۴-الف: تیک دستی تأیید فاکتور اجباری است قبل از ارسال
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). <b>400</b>.
/// </summary>
public sealed class PaymentRequestInvoiceNotApprovedException : Exception
{
    public PaymentRequestInvoiceNotApprovedException(Guid paymentRequestId)
        : base($"Payment request {paymentRequestId} has an invoice reference but INVOICE_APPROVED is false.")
    {
        PaymentRequestId = paymentRequestId;
    }

    public Guid PaymentRequestId { get; }

    public string PublicDetail => "شمارهٔ فاکتور ثبت شده اما تیک تأیید فاکتور زده نشده است.";
}
