namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown on Submit when <c>DUE_DATE</c> already fell in the past — خزانه‌داری، بخش ۴-الف
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). <b>400</b> — this specific value, right now, does
/// not fit, same shape as the petty-cash §4 Submit rules.
/// </summary>
public sealed class PaymentRequestDueDatePastException : Exception
{
    public PaymentRequestDueDatePastException(Guid paymentRequestId, string dueDate)
        : base($"Payment request {paymentRequestId}'s due date {dueDate} is in the past.")
    {
        PaymentRequestId = paymentRequestId;
        DueDate = dueDate;
    }

    public Guid PaymentRequestId { get; }

    public string DueDate { get; }

    public string PublicDetail => "تاریخ سررسید نمی‌تواند گذشته باشد.";
}
