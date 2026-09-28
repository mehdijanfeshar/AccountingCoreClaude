using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// «ترمیم/شارژ تنخواه» (RCH-xxxxx) — بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>، صفحهٔ ۹
/// پاورپوینت، ۲۰۲۶-۰۹-۲۸). جدول جانبی جدید (پیشوند <c>TB_PC_</c>، همان استثنای صریح صاحب پروژه بر
/// قانون «هیچ جدول جدیدی» که بخش‌های قبلی استفاده کرده‌اند).
///
/// ۱:۱ با <see cref="TB_CHARGEANDCOST_HEAD"/> نوع <see cref="ChargeAndCostType.Charge"/>
/// (<c>UK_PC_REPLENISHMENT</c> روی <see cref="CHARGEANDCOSTHEAD_ID"/>) — همان الگوی
/// <see cref="TB_PC_EXPENSE_DOC"/>/<see cref="TB_CHARGEANDCOST_HEAD"/> نوع هزینه‌کرد، فقط اینجا
/// <c>ACCOUNT_ID</c> روی خودِ سرسند Legacy (حساب بانکی مبدأ) ست می‌شود، نه روی این جدول. هر
/// صورت‌هزینهٔ منظورشده یک ردیف <see cref="TB_CHARGE_LINK_COST"/> دارد
/// (<c>CHARGE_ID</c> → <see cref="CHARGEANDCOSTHEAD_ID"/>، <c>COST_ID</c> →
/// <c>TB_CHARGEANDCOST_DETAIL.ID</c> همان صورت‌هزینه).
///
/// <c>ID</c> بدون <c>DEFAULT sys_guid()</c> (ریسک #۱۱)؛ همیشه application-side تولید می‌شود.
/// </summary>
public partial class TB_PC_REPLENISHMENT
{
    public Guid ID { get; set; }

    /// <summary>
    /// اشاره به <see cref="TB_CHARGEANDCOST_HEAD"/> نوع <see cref="ChargeAndCostType.Charge"/> —
    /// <c>UK_PC_REPLENISHMENT</c> روی این ستون یکتاست.
    /// </summary>
    public Guid CHARGEANDCOSTHEAD_ID { get; set; }

    public Guid FUND_ID { get; set; }

    /// <summary>
    /// نمایش کامل («<c>RCH-</c>» + شماره)، برخلاف <c>TB_CHARGEANDCOST_HEAD.CHARGEANDCOST_CODE</c>
    /// (که پیشوند را در Read model اضافه می‌کند) — اینجا کد جدول جانبی خودش شمارهٔ کامل را نگه
    /// می‌دارد، چون این جدول Legacy نیست.
    /// </summary>
    public string CODE { get; set; } = null!;

    public PettyCashPaymentMethod PAYMENT_METHOD { get; set; }

    public PettyCashReplenishmentState STATE { get; set; }

    public decimal TOTAL_AMOUNT { get; set; }

    public string? NOTE { get; set; }

    public DateTime? PAID_DATE { get; set; }

    public string? PAID_BY_USERID { get; set; }

    public string? APPROVED_BY_USERID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_CHARGEANDCOST_HEAD? CHARGEANDCOSTHEAD { get; set; }

    public virtual TB_PC_FUND? FUND { get; set; }
}
