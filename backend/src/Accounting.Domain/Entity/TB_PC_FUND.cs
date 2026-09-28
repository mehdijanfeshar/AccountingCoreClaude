using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// «تنخواه» — تعریف مستقل ماژول تنخواه و خزانه‌داری (فاز ۴۴)، جایگزین کامل
/// <c>TB_REVOLVING_FUND</c>/<c>TB_PC_FUND_SETTING</c> برای این ماژول به تصمیم صریح صاحب پروژه
/// (۲۰۲۶-۰۹-۲۸، <c>docs/tankhah-khazaneh-module.md</c> §۰). این استثنای دوم بر قانون «Legacy-as-
/// Domain» است — فقط برای همین ماژول: از <c>TB_REVOLVING_FUND</c> یا هر تنخواهٔ قدیمی دیگر هیچ
/// استفاده‌ای نمی‌شود؛ هستهٔ داده اینجاست، نه در Legacy.
///
/// جدول جدید «جانبی» (پیشوند <c>TB_PC_</c>)، همان استثنای صریح صاحب پروژه بر قانون «هیچ جدول
/// جدیدی». <c>ID</c> بدون <c>DEFAULT sys_guid()</c> (ریسک #۱۱)؛ همیشه application-side تولید
/// می‌شود. <c>UK_PC_FUND_CODE</c> روی (<see cref="VAHEDCODE"/>, <see cref="CODE"/>) یکتاست —
/// بدون <c>YEAR</c>، چون این ماژول تنخواه را نهادی پایا می‌داند، نه چیزی که هر سال از نو تعریف شود.
/// </summary>
public partial class TB_PC_FUND
{
    public Guid ID { get; set; }

    public string CODE { get; set; } = null!;

    public string NAME { get; set; } = null!;

    /// <summary>فضای هویتی <c>ICurrentUser.UserId</c>/<c>ADDUSERID</c> — تنخواه‌دار مسئول.</summary>
    public string CUSTODIAN_USERID { get; set; } = null!;

    public string? CUSTODIAN_NAME { get; set; }

    /// <summary>سقف تنخواه — پیش‌تر <c>TB_REVOLVING_FUND.DEFAULTAMOUNT</c>.</summary>
    public decimal CEILING { get; set; }

    /// <summary>سقف هر سند — پیش‌تر <c>TB_PC_FUND_SETTING.PER_DOC_LIMIT</c>، آنجا اختیاری بود، اینجا الزامی.</summary>
    public decimal PER_DOC_LIMIT { get; set; }

    /// <summary>سقف اختیار تأیید نهایی نقش <see cref="Accounting.Domain.ValueObjects.PettyCashRole.FinanceManager"/>
    /// («تا ۵۰۰ م» صفحهٔ ۱۳ پاورپوینت) — بیشتر از این فقط <see cref="Accounting.Domain.ValueObjects.PettyCashRole.ChiefExecutive"/>
    /// می‌تواند تأیید نهایی کند. تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸).</summary>
    public decimal FINANCE_MANAGER_APPROVAL_LIMIT { get; set; }

    public int? ALERT_THRESHOLD_PERCENT { get; set; }

    public Guid? ACCOUNTCODE_ID { get; set; }

    public PettyCashSettlementPeriod? SETTLEMENT_PERIOD { get; set; }

    public bool IS_ACTIVE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_ACCOUNTCODE? ACCOUNTCODE { get; set; }
}
