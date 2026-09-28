namespace Accounting.Domain.ValueObjects;

/// <summary>
/// State machine for a ترمیم/شارژ تنخواه (<c>TB_PC_REPLENISHMENT.STATE</c>) — بخش ۳-الف
/// (<c>docs/tankhah-khazaneh-module.md</c>، صفحهٔ ۹ پاورپوینت، ۲۰۲۶-۰۹-۲۸).
///
/// <c>Draft → PendingFinanceManager → PendingTreasurer → Paid</c>, with <see cref="Rejected"/>
/// reachable from either Pending state. There is deliberately no <c>Settled</c> value here — that
/// belongs to <see cref="PettyCashDocState.Settled"/> on the underlying صورت‌هزینه documents
/// (بخش ۳-ب، تسویهٔ دوره), which this batch does not implement.
/// </summary>
public enum PettyCashReplenishmentState
{
    /// <summary>پیش‌نویس — ساخته شده (و اسناد تأییدشده لینک شده‌اند)، هنوز ارسال نشده. تنها وضعیت
    /// قابل حذف.</summary>
    Draft = 1,

    /// <summary>در انتظار تأیید مدیر مالی.</summary>
    PendingFinanceManager = 2,

    /// <summary>در انتظار اقدام خزانه‌دار (اجرای پرداخت).</summary>
    PendingTreasurer = 3,

    /// <summary>پرداخت‌شده — <c>record-payment</c> اجرا شد. سند حسابداری در این بخش صادر
    /// نمی‌شود (بخش ۴+، اجرای پرداخت خزانه).</summary>
    Paid = 4,

    /// <summary>ردشده — پایانی؛ لینک اسناد این ترمیم نرم‌حذف می‌شوند تا اسناد دوباره قابل ترمیم
    /// باشند.</summary>
    Rejected = 5,
}
