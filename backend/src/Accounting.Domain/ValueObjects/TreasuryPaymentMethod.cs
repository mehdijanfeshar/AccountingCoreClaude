namespace Accounting.Domain.ValueObjects;

/// <summary>
/// روش پرداخت — <c>TB_TR_PAYMENT_REQUEST.PAYMENT_METHOD</c>. مقدارها و ترتیبشان عمداً با
/// <see cref="PettyCashPaymentMethod"/> یکی نیست (این‌جا ساتنا/پایا اول‌اند) — جدول جانبی مستقل،
/// طراحی خودش (<c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// </summary>
public enum TreasuryPaymentMethod
{
    Satna = 1,
    Paya = 2,
    Cheque = 3,
    Cash = 4,
}
