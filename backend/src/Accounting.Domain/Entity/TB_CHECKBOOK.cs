using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

public partial class TB_CHECKBOOK
{
    public Guid ID { get; set; }

    public Guid ACCOUNT_ID { get; set; }

    public string? CHECKBOOK_TITLE { get; set; }

    public string CHECKBOOK_DATE { get; set; } = null!;

    public string FROMCHECKNUMBER { get; set; } = null!;

    public string TOCHECKNUMBER { get; set; } = null!;

    public Guid? CHECKTYPE_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public bool ISDELETED { get; set; }

    /// <summary>
    /// نوع چک — <see cref="ValueObjects.CheckType"/> (۱=چک صوری, ۲=چک واقعی). تا فاز ۲۷ (بچ ۲)
    /// به‌اشتباه <c>bool?</c> بود؛ رجوع به <c>docs/centralaccount-business-reference.md</c> §۲۴-۱.
    /// ⚠️ §۲۴-۳: در پروژهٔ مرجع این ستون سمت سرور همیشه ثابت <c>CheckType.real</c> است و هرگز
    /// ورودی فراخوان نیست — این فاز فقط نوع CLR را اصلاح کرد، نه caller-supplied بودن آن.
    /// </summary>
    public CheckType? CHECKBOOK_TYPE { get; set; }

    public string? SERIAL { get; set; }

    public virtual TB_ACCOUNT ACCOUNT { get; set; } = null!;

    public virtual TB_CHECK_TYPE? CHECKTYPE { get; set; }

    public virtual ICollection<TB_CHECK> TB_CHECKs { get; set; } = new List<TB_CHECK>();
}
