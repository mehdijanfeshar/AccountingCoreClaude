namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Audit-trail action recorded on <c>TB_TR_TRANSFER_EVENT.ACTION</c> — همان الگوی
/// <see cref="PaymentRequestEventAction"/>. هر تغییر وضعیت روی <c>TB_TR_TRANSFER</c> دقیقاً یک
/// ردیف اینجا، در همان تراکنش، می‌گیرد. Insert-only.
/// </summary>
public enum TransferEventAction
{
    Create = 1,
    Update = 2,
    Delete = 3,
    Submit = 4,

    /// <summary>خزانه‌دار تأیید کرد — سند «انتقال» صادر و وضعیت <see cref="TransferState.Executed"/> شد.</summary>
    Approve = 5,

    /// <summary>بازگشت به ثبت‌کننده برای اصلاح — دلیل اجباری در <c>NOTE</c>.</summary>
    Return = 6,

    /// <summary>رد پایانی — دلیل اجباری در <c>NOTE</c>.</summary>
    Reject = 7,
}
