namespace Accounting.Domain.ValueObjects;

/// <summary>
/// دورهٔ تسویهٔ تنخواه (<c>TB_PC_FUND.SETTLEMENT_PERIOD</c> از ۲۰۲۶-۰۹-۲۸؛ تا آن زمان
/// <c>TB_PC_FUND_SETTING.SETTLEMENT_PERIOD</c>) — مقادیر عیناً از
/// <c>docs/tankhah-khazaneh-module.md</c> §۳. مصرف‌کنندهٔ واقعی‌اش (تسویهٔ دوره) در بخش سوم ماژول
/// ساخته می‌شود؛ بخش ۱ فقط این تنظیم را ذخیره می‌کند.
/// </summary>
public enum PettyCashSettlementPeriod
{
    /// <summary>ماهانه.</summary>
    Monthly = 1,

    /// <summary>فصلی.</summary>
    Quarterly = 2,
}
