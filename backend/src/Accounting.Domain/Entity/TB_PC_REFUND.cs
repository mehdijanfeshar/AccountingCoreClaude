using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// «استرداد وجه تنخواه» — بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>، صفحهٔ ۱۱ پاورپوینت،
/// ۲۰۲۶-۰۹-۲۸). جدول جانبی جدید (پیشوند <c>TB_PC_</c>)، بدون معادل Legacy — تنخواه‌دار مبلغی را
/// نقداً/به‌حساب به سازمان برمی‌گرداند (مثلاً مانده‌ای که مصرف نشده)، بدون اینکه یک صورت‌هزینه یا
/// ترمیم باشد.
///
/// چه کسی مجاز به ثبت است را <see cref="TB_PC_FUND.REFUND_RECORDER"/> تعیین می‌کند، نه کد ثابت
/// (تصمیم صاحب پروژه). <c>ID</c> بدون <c>DEFAULT sys_guid()</c> (ریسک #۱۱)؛ همیشه
/// application-side تولید می‌شود.
/// </summary>
public partial class TB_PC_REFUND
{
    public Guid ID { get; set; }

    public Guid FUND_ID { get; set; }

    /// <summary>نمایش کامل («<c>REF-</c>» + شماره) — همان الگوی <see cref="TB_PC_REPLENISHMENT.CODE"/>.</summary>
    public string CODE { get; set; } = null!;

    public decimal AMOUNT { get; set; }

    public string? REASON { get; set; }

    /// <summary>تاریخ شمسی <c>YYYYMMDD</c> — هم‌الگوی <c>TB_PC_EXPENSE_DOC.INVOICE_DATE</c>.</summary>
    public string REFUND_DATE { get; set; } = null!;

    public string RECORDED_BY_USERID { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_PC_FUND? FUND { get; set; }
}
