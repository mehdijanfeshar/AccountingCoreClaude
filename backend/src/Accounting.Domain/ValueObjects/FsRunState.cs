namespace Accounting.Domain.ValueObjects;

/// <summary>
/// وضعیت اجرای تهیهٔ صورت‌های مالی — <c>TB_FS_RUN.STATE</c> (سند منبع §۶ و §۱۱). گذارها سمت Application
/// (قاعدهٔ ۲ CLAUDE.md): Draft ⇄ InReview (ارسال/برگشت) → Approved → Published؛ انتشار نسخهٔ تازه، منتشرشدهٔ
/// قبلی همان دوره را Superseded می‌کند. «برگشت» وضعیت جدا ندارد — به Draft برمی‌گردد (دلیلش در تاریخچه).
/// </summary>
public enum FsRunState
{
    /// <summary>پیش‌نویس — تازه محاسبه‌شده یا برگشتی.</summary>
    Draft = 1,
    /// <summary>ارسال‌شده برای بازبینی.</summary>
    InReview = 2,
    /// <summary>تأییدشده — آمادهٔ انتشار.</summary>
    Approved = 3,
    /// <summary>منتشرشده — نسخهٔ رسمی دوره.</summary>
    Published = 4,
    /// <summary>جایگزین‌شده با اجرای تازه‌تر (مقادیر دستی یا انتشار نسخهٔ بعدی).</summary>
    Superseded = 5,
}
