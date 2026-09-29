namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Audit-trail action recorded on <c>TB_TR_PAYMENT_REQUEST_EVENT.ACTION</c> — همان الگوی
/// <see cref="PettyCashDocAction"/>. هر تغییر وضعیت روی <c>TB_TR_PAYMENT_REQUEST</c> دقیقاً یک
/// ردیف اینجا، در همان تراکنش، می‌گیرد.
/// </summary>
public enum PaymentRequestEventAction
{
    Create = 1,
    Update = 2,
    Submit = 3,
    Delete = 4,

    /// <summary>گذار یک مرحله به مرحلهٔ تأیید بعدی (توسط مدیر واحد/مدیر مالی/مدیرعامل) — تکی یا از
    /// تأیید گروهی.</summary>
    Approve = 5,

    /// <summary>بازگشت به ثبت‌کننده برای اصلاح — دلیل اجباری در <c>NOTE</c>.</summary>
    Return = 6,

    /// <summary>رد پایانی — دلیل اجباری در <c>NOTE</c>.</summary>
    Reject = 7,
}
