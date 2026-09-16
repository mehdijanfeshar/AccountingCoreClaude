using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

public partial class TB_PAYRECIVHEAD
{
    public Guid ID { get; set; }

    public string PAYRECIVCODE { get; set; } = null!;

    public string PAYRECIVDATE { get; set; } = null!;

    public string PAYRECIVDESCRIPTION { get; set; } = null!;

    /// <summary>
    /// نوع سند دریافت/پرداخت — <see cref="ValueObjects.PayRecivType"/> (۱=پرداخت, ۲=دریافت,
    /// ۳=همه). تا فاز ۲۷ (بچ ۲) به‌اشتباه <c>bool?</c> بود؛ مقدار سوم (۳=همه) با آن نوع اصلاً
    /// قابل‌دسترس نبود. رجوع به <c>docs/centralaccount-business-reference.md</c> §۲۴-۱ ردیف ۱۴.
    /// </summary>
    public PayRecivType? PAYRECIVTYPE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public Guid? VOUCHERSHEAD_ID { get; set; }

    public virtual ICollection<TB_PAYRECIVDETAIL> TB_PAYRECIVDETAILs { get; set; } = new List<TB_PAYRECIVDETAIL>();

    public virtual TB_VOUCHERSHEAD? VOUCHERSHEAD { get; set; }
}
