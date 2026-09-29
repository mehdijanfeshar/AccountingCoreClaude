namespace Accounting.Domain.ValueObjects;

/// <summary>
/// State machine for <c>TB_TR_TRANSFER.STATE</c> — خزانه‌داری، بخش ۴-ج (انتقال وجه)
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰، ۲۰۲۶-۰۹-۲۹). Single approval stage (خزانه‌دار) —
/// unlike <see cref="PaymentRequestState"/>'s multi-role chain, there is only one Pending state
/// here. <see cref="Draft"/>/<see cref="Returned"/> → <c>submit</c> → <see cref="PendingTreasurer"/>
/// → <c>approve</c> → <see cref="Executed"/> (issues the GL voucher) or <c>return</c>/<c>reject</c>.
/// </summary>
public enum TransferState
{
    /// <summary>پیش‌نویس — قابل ویرایش/حذف، همراه با <see cref="Returned"/>.</summary>
    Draft = 1,

    /// <summary>در انتظار اقدام خزانه‌دار.</summary>
    PendingTreasurer = 2,

    /// <summary>اجراشده — سند GL موقت صادر شده. پایانی.</summary>
    Executed = 3,

    /// <summary>برگشتی — با دلیل اجباری، به ثبت‌کننده برای اصلاح (<c>submit</c> دوباره از همین
    /// وضعیت آغاز می‌شود).</summary>
    Returned = 4,

    /// <summary>ردشده — پایانی، با دلیل اجباری.</summary>
    Rejected = 5,
}
