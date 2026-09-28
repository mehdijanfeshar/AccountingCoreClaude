using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// «دورهٔ تسویهٔ تنخواه» — بخش ۳-ب (<c>docs/tankhah-khazaneh-module.md</c> §۹، ۲۰۲۶-۰۹-۲۸). جدول
/// جدید «جانبی» (پیشوند <c>TB_PC_</c>)، همان استثنای صریح صاحب پروژه بر قانون «هیچ جدول جدیدی».
/// بدون معادل Legacy — <c>TB_REVOLVING_FUND</c> و پروژهٔ مرجع مفهوم «دورهٔ تسویه» ندارند.
///
/// <c>ID</c> بدون <c>DEFAULT sys_guid()</c> (ریسک #۱۱)؛ همیشه application-side تولید می‌شود.
/// <c>UK_PC_SETTLEMENT_PERIOD</c> روی (<see cref="FUND_ID"/>, <see cref="PERIOD_START"/>,
/// <see cref="PERIOD_END"/>) یکتاست — یک تنخواه در یک بازهٔ مشخص فقط یک دوره دارد.
///
/// دوره‌ها باید پشت‌سرهم نهایی شوند؛ این invariant در DB اعمال نشده (بدون CHECK) — Application
/// (<c>PettyCashSettlementPeriodCalculator</c>) آن را با محاسبهٔ «دورهٔ بعدی مجاز» تضمین می‌کند،
/// نه با یک constraint.
/// </summary>
public partial class TB_PC_SETTLEMENT_PERIOD
{
    public Guid ID { get; set; }

    public Guid FUND_ID { get; set; }

    /// <summary>شمسی <c>YYYYMMDD</c> — ابتدای دوره.</summary>
    public string PERIOD_START { get; set; } = null!;

    /// <summary>شمسی <c>YYYYMMDD</c> — انتهای دوره (شاملِ خودش).</summary>
    public string PERIOD_END { get; set; } = null!;

    /// <summary>مانده نقد محاسبه‌شدهٔ ابتدای دوره — از پایان دورهٔ Final قبلی، یا برای اولین دوره
    /// همان سقف اولیهٔ تنخواه (همان منطق گزارش گردش، §۳-الف).</summary>
    public decimal OPENING_BALANCE { get; set; }

    /// <summary>شمارش دستی صندوق — با <c>POST settlement/count</c> ثبت می‌شود؛ پیش از نهایی‌سازی
    /// باید برابر مانده نقد محاسبه‌شدهٔ پایان دوره باشد.</summary>
    public decimal? COUNTED_BALANCE { get; set; }

    public PettyCashSettlementState STATE { get; set; }

    /// <summary>سند GL تسویه — فقط وقتی <see cref="STATE"/> برابر <see cref="PettyCashSettlementState.Final"/>
    /// است مقدار دارد.</summary>
    public Guid? VOUCHERSHEAD_ID { get; set; }

    public string? FINALIZED_BY_USERID { get; set; }

    public DateTime? FINALIZED_DATE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_PC_FUND? FUND { get; set; }

    public virtual TB_VOUCHERSHEAD? VOUCHERSHEAD { get; set; }
}
