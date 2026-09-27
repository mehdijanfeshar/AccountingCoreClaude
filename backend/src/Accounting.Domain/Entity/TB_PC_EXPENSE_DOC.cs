using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// «صورت‌هزینهٔ تنخواه» (TH-xxxxx) — لایهٔ جانبی روی هستهٔ Legacy: هر ردیف دقیقاً یک
/// <see cref="TB_CHARGEANDCOST_HEAD"/> نوع هزینه‌کرد (+ یک <see cref="TB_CHARGEANDCOST_DETAIL"/>)
/// را همراهی می‌کند و فروشنده/فاکتور/ارزش‌افزوده/وضعیت هفت‌تایی/تنخواه را نگه می‌دارد که در
/// schema Legacy جایی ندارند (<c>docs/tankhah-khazaneh-module.md</c> §۱/§۳).
///
/// ۱:۱ با <see cref="TB_CHARGEANDCOST_HEAD"/> — <c>UK_PC_EXPENSE_DOC</c> روی
/// <c>CHARGEANDCOSTHEAD_ID</c>. <c>REVOLVINGFUND_ID</c> اینجا نگه داشته می‌شود، نه روی ردیف
/// Legacy — یافتهٔ زندهٔ §۱: هزینه‌کردهای موجود هیچ <c>REVOLVINGFUND_ID</c> ای در
/// <c>TB_CHARGEANDCOST_DETAIL</c> ندارند. <c>ID</c> بدون <c>DEFAULT sys_guid()</c> (ریسک #۱۱).
/// </summary>
public partial class TB_PC_EXPENSE_DOC
{
    public Guid ID { get; set; }

    public Guid CHARGEANDCOSTHEAD_ID { get; set; }

    public Guid REVOLVINGFUND_ID { get; set; }

    public PettyCashDocState DOC_STATE { get; set; }

    public string? VENDOR_NAME { get; set; }

    public string? VENDOR_NATIONAL_ID { get; set; }

    public string? INVOICE_NO { get; set; }

    public string? INVOICE_DATE { get; set; }

    public PettyCashEvidenceType? EVIDENCE_TYPE { get; set; }

    public decimal? AMOUNT_BEFORE_TAX { get; set; }

    public decimal? VAT_AMOUNT { get; set; }

    public DateTime? SUBMITTED_DATE { get; set; }

    public string? RETURN_DEADLINE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_CHARGEANDCOST_HEAD? CHARGEANDCOSTHEAD { get; set; }

    public virtual TB_REVOLVING_FUND? REVOLVINGFUND { get; set; }
}
