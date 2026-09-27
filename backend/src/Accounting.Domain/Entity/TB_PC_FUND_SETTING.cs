using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// «تنظیمات تنخواه» — سقف هر سند، آستانهٔ هشدار، تنخواه‌دار و دورهٔ تسویه برای یک
/// <see cref="TB_REVOLVING_FUND"/>. جدول جدید «جانبی» (پیشوند <c>TB_PC_</c>)، استثنای صریح
/// صاحب پروژه بر قانون «هیچ جدول جدیدی» — فقط برای دادهٔ ماژول تنخواه که در schema Legacy جایی
/// ندارد (<c>docs/tankhah-khazaneh-module.md</c> §۰/§۳).
///
/// ۱:۱ با <see cref="TB_REVOLVING_FUND"/> — <c>UK_PC_FUND_SETTING</c> روی <c>REVOLVINGFUND_ID</c>.
/// <c>ID</c> بدون <c>DEFAULT sys_guid()</c> (ریسک #۱۱)؛ همیشه application-side تولید می‌شود.
/// </summary>
public partial class TB_PC_FUND_SETTING
{
    public Guid ID { get; set; }

    public Guid REVOLVINGFUND_ID { get; set; }

    public string? CUSTODIAN_USERID { get; set; }

    public string? CUSTODIAN_NAME { get; set; }

    public decimal? PER_DOC_LIMIT { get; set; }

    public int? ALERT_THRESHOLD_PERCENT { get; set; }

    public PettyCashSettlementPeriod? SETTLEMENT_PERIOD { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_REVOLVING_FUND? REVOLVINGFUND { get; set; }
}
