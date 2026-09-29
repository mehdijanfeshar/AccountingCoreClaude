namespace Accounting.Domain.ValueObjects;

/// <summary>
/// State machine for <c>TB_TR_PAYMENT_REQUEST.REQUEST_STATE</c> — خزانه‌داری، بخش ۴-الف
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰، ۲۰۲۶-۰۹-۲۸). Linear approval chain
/// <see cref="Draft"/> → <see cref="PendingUnitManager"/> → <see cref="PendingFinanceManager"/> →
/// (فقط اگر مبلغ خالص قابل‌پرداخت از آستانهٔ مدیرعامل بیشتر باشد) <see cref="PendingCeo"/> →
/// <see cref="ReadyForExecution"/>. از هر وضعیت Pending، هم <see cref="Returned"/> (اصلاح و ارسال
/// مجدد) و هم <see cref="Rejected"/> (پایانی) قابل دسترس‌اند.
/// </summary>
public enum PaymentRequestState
{
    /// <summary>پیش‌نویس — تنها وضعیت قابل ویرایش/حذف آزاد، همراه با <see cref="Returned"/>.</summary>
    Draft = 1,

    /// <summary>در انتظار تأیید مدیر واحد.</summary>
    PendingUnitManager = 2,

    /// <summary>در انتظار تأیید مدیر مالی.</summary>
    PendingFinanceManager = 3,

    /// <summary>در انتظار تأیید مدیرعامل — فقط وقتی <c>NET_PAYABLE_AMOUNT</c> از
    /// <c>TB_TR_SETTING.CEO_APPROVAL_THRESHOLD</c> واحد بیشتر باشد.</summary>
    PendingCeo = 4,

    /// <summary>آمادهٔ اجرا — گام بعدی (بخش ۴-ب) اجرای پرداخت و ثبت در Legacy
    /// <c>TB_PAYRECIVHEAD/DETAIL</c> است؛ اینجا صادر نمی‌شود.</summary>
    ReadyForExecution = 5,

    /// <summary>برگشتی — با دلیل اجباری، به ثبت‌کننده برای اصلاح و ارسال مجدد
    /// (<c>submit</c> دوباره از همین وضعیت زنجیرهٔ تأیید را از مدیر واحد شروع می‌کند).</summary>
    Returned = 6,

    /// <summary>ردشده — پایانی، با دلیل اجباری.</summary>
    Rejected = 7,

    /// <summary>بخش ۴-ب — اجرا شد؛ سند «پرداخت» (شمارهٔ ۲) صادر و <c>TB_PAYRECIVHEAD/DETAIL</c>
    /// نوشته شد. پایانی — از اینجا برگشت/رد ممکن نیست (سند شناسایی بدهی از قبل صادر شده).</summary>
    Executed = 8,

    /// <summary>بخش ۴-ب — موقتاً معلق (دلیل اجباری). فقط از <see cref="ReadyForExecution"/> و فقط
    /// دوباره به همان‌جا برمی‌گردد (<c>resume</c>)؛ سند شناسایی بدهی دست‌نخورده می‌ماند.</summary>
    Suspended = 9,
}
