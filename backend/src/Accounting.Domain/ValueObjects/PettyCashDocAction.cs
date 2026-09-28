namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Audit-trail action recorded on <c>TB_PC_DOC_EVENT.ACTION</c> for a صورت‌هزینهٔ تنخواه. Chunk 1
/// wrote <see cref="Create"/>, <see cref="Update"/>, <see cref="Submit"/> and <see cref="Delete"/>.
/// Chunk 2 (بخش ۲، <c>docs/tankhah-khazaneh-module.md</c> تصمیم‌های بخش ۲) fills the values that
/// were deliberately reserved back then: <see cref="StartReview"/>, <see cref="Approve"/>,
/// <see cref="Return"/> and <see cref="Reject"/>. Bulk approve reuses <see cref="FinalApprove"/> —
/// one event row per document, not a separate action value.
///
/// تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸، تأیید دومرحله‌ای، صفحهٔ ۱۲ پاورپوینت) adds <see cref="Verify"/> (کنترل
/// بازرس — وضعیت عوض نمی‌شود) and <see cref="FinalApprove"/> (تأیید نهایی — همان گذار
/// PendingReview→Approved که پیش‌تر <see cref="Approve"/> انجام می‌داد؛ آن مقدار برای دادهٔ تاریخی
/// نگه داشته شده، دیگر تولید نمی‌شود).
/// </summary>
public enum PettyCashDocAction
{
    /// <summary>سند صورت‌هزینه ساخته شد (پیش‌نویس یا مستقیماً ارسال‌شده).</summary>
    Create = 1,

    /// <summary>سند صورت‌هزینه (در وضعیت پیش‌نویس یا برگشتی) ویرایش شد.</summary>
    Update = 2,

    /// <summary>سند از پیش‌نویس/برگشتی به «جدید» ارسال شد.</summary>
    Submit = 3,

    /// <summary>سند (فقط از پیش‌نویس) حذف نرم شد.</summary>
    Delete = 4,

    /// <summary>سند از «جدید» به «در انتظار بررسی» منتقل شد (بررسی‌کننده کارتابل را باز کرد).</summary>
    StartReview = 5,

    /// <summary>سند از «در انتظار بررسی» به «تأییدشده» منتقل شد (تکی یا از تأیید گروهی).</summary>
    Approve = 6,

    /// <summary>سند از «در انتظار بررسی» به «برگشتی» منتقل شد.</summary>
    Return = 7,

    /// <summary>سند از «در انتظار بررسی» به «ردشده» منتقل شد.</summary>
    Reject = 8,

    /// <summary>کنترل سند توسط بازرس تأیید شد — وضعیت تغییر نمی‌کند (from=to=PendingReview)؛
    /// <c>TB_PC_EXPENSE_DOC.VERIFIED_BY_USERID</c>/<c>VERIFIED_DATE</c> ست می‌شوند.</summary>
    Verify = 9,

    /// <summary>تأیید نهایی — سند از «در انتظار بررسی» به «تأییدشده» منتقل شد (تکی یا از تأیید
    /// گروهی). جایگزین تولید <see cref="Approve"/> از تکمیل بخش ۲ به بعد.</summary>
    FinalApprove = 10,

    // 11..: Settle — بخش ۳، عمداً هنوز تعریف نشده است.
}
