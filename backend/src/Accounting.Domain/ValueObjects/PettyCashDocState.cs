namespace Accounting.Domain.ValueObjects;

/// <summary>
/// وضعیت هفت‌تایی صورت‌هزینهٔ تنخواه (<c>TB_PC_EXPENSE_DOC.DOC_STATE</c>) — طراحی و شماره‌گذاری
/// مقادیر عیناً از <c>docs/tankhah-khazaneh-module.md</c> §۲ (که از پاورپوینت طراحی، ص ۳، گرفته
/// شده) — حدس زده نشده‌اند.
///
/// نگاشت به وضعیت Legacy <c>TB_CHARGEANDCOST_HEAD.STATUS</c> فقط در یک جا انجام می‌شود:
/// <see cref="Accounting.Application.Common.Security.PettyCashStatusMap"/>. هیچ handler ای این
/// enum را مستقیماً به <see cref="ChargeAndCostStatus"/> تبدیل نمی‌کند.
/// </summary>
public enum PettyCashDocState
{
    /// <summary>پیش‌نویس — ذخیره شده، هنوز ارسال نشده. تنها وضعیت قابل حذف؛ همراه با
    /// <see cref="Returned"/> تنها دو وضعیت قابل ویرایش.</summary>
    Draft = 1,

    /// <summary>جدید — ارسال شده، هنوز باز نشده.</summary>
    New = 2,

    /// <summary>در انتظار بررسی — در کارتابل بازرس (بخش ۲).</summary>
    PendingReview = 3,

    /// <summary>برگشتی — نیازمند اصلاح تنخواه‌دار. قابل ویرایش و قابل ارسال مجدد.</summary>
    Returned = 4,

    /// <summary>تأییدشده — منتظر ترمیم (بخش ۳).</summary>
    Approved = 5,

    /// <summary>ردشده — پایانی، بدون ترمیم.</summary>
    Rejected = 6,

    /// <summary>تسویه‌شده — در سند تسویهٔ دوره منظور شد (بخش ۳).</summary>
    Settled = 7,
}
