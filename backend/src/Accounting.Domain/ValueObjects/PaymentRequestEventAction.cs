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

    /// <summary>بخش ۴-ب — سند «شناسایی بدهی» (شمارهٔ ۱) در همان لحظهٔ گذار به
    /// <see cref="PaymentRequestState.ReadyForExecution"/> صادر شد. <c>NOTE</c> شمارهٔ سند را
    /// حمل می‌کند؛ <c>FROM_STATE</c>/<c>TO_STATE</c> هر دو <see cref="PaymentRequestState.ReadyForExecution"/>‌اند
    /// (تغییر وضعیتی نیست، فقط یک رویداد الحاقی کنار رویداد Approve همان گذار).</summary>
    LiabilityVoucherIssued = 8,

    /// <summary>بخش ۴-ب — اجرای پرداخت: سند «پرداخت» (شمارهٔ ۲) صادر و
    /// <c>TB_PAYRECIVHEAD/DETAIL</c> نوشته شد.</summary>
    Execute = 9,

    /// <summary>بخش ۴-ب — تعلیق موقت. دلیل اجباری در <c>NOTE</c>.</summary>
    Suspend = 10,

    /// <summary>بخش ۴-ب — رفع تعلیق، بازگشت به <see cref="PaymentRequestState.ReadyForExecution"/>.</summary>
    Resume = 11,
}
