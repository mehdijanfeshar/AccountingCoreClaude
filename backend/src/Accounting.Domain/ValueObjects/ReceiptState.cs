namespace Accounting.Domain.ValueObjects;

/// <summary>
/// State machine for <c>TB_TR_RECEIPT.STATE</c> — خزانه‌داری، بخش ۴-ج (دریافت وجه)
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰، ۲۰۲۶-۰۹-۲۹). Deliberately a much shorter chain
/// than <see cref="PaymentRequestState"/> — no approval chain, just draft → registered (issues the
/// GL voucher + Legacy <c>TB_PAYRECIVHEAD</c>) or draft → cancelled (owner decision: «keep it
/// simple» — a registered receipt is corrected via its voucher, not reopened here).
/// </summary>
public enum ReceiptState
{
    /// <summary>پیش‌نویس — تنها وضعیت قابل ویرایش/حذف/لغو.</summary>
    Draft = 1,

    /// <summary>ثبت‌شده — سند GL موقت + <c>TB_PAYRECIVHEAD/DETAIL</c> صادر شده‌اند. پایانی.</summary>
    Registered = 2,

    /// <summary>لغوشده — فقط از پیش‌نویس ممکن است. پایانی.</summary>
    Cancelled = 3,
}
