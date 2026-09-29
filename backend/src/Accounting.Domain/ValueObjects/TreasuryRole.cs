namespace Accounting.Domain.ValueObjects;

/// <summary>
/// A کاربر's role within one واحد's خزانه‌داری workflow — <c>TB_TR_ROLE.ROLE</c>. Deliberately a
/// <b>separate</b> role list from <see cref="PettyCashRole"/> (صاحب پروژه، ۲۰۲۶-۰۹-۲۸): a fresh
/// per-واحد list, independent of <c>TB_PC_REVIEWER</c>. Only <see cref="UnitManager"/>,
/// <see cref="FinanceManager"/> and <see cref="Ceo"/> currently gate a
/// <c>TB_TR_PAYMENT_REQUEST</c> state transition; <see cref="SeniorAccountant"/> and
/// <see cref="Treasurer"/> are reserved for بخش ۴-ب/۴-ج/۴-د (اجرای پرداخت، دریافت/انتقال).
/// </summary>
public enum TreasuryRole
{
    /// <summary>مدیر واحد — گام اول تأیید (<see cref="PaymentRequestState.PendingUnitManager"/>).</summary>
    UnitManager = 1,

    /// <summary>مدیر مالی — گام دوم تأیید؛ تنها نقش مجاز به تعریف/ویرایش <c>TB_TR_SETTING</c> و
    /// مدیریت <c>TB_TR_ROLE</c> (با استثنای bootstrap).</summary>
    FinanceManager = 2,

    /// <summary>مدیرعامل — گام سوم تأیید، فقط وقتی مبلغ از آستانهٔ واحد بیشتر باشد.</summary>
    Ceo = 3,

    /// <summary>حسابدار ارشد — رزرو‌شده برای بخش‌های بعدی.</summary>
    SeniorAccountant = 4,

    /// <summary>خزانه‌دار — رزرو‌شده برای اجرای پرداخت (بخش ۴-ب).</summary>
    Treasurer = 5,
}
