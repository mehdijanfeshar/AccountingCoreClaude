using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

public partial class TB_TAFSIL_GROUP
{
    public Guid ID { get; set; }

    public string TAFSILGROUP_CODE { get; set; } = null!;

    public string TAFSILGROUP_NAME { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    /// <summary>
    /// نوع شخص — <see cref="ValueObjects.PersonTypes"/> (۱=حقیقی, ۲=حقوقی, ۳=سایر). تا فاز ۲۷
    /// (بچ ۱) به‌اشتباه <c>bool?</c> بود؛ رجوع به <c>docs/centralaccount-business-reference.md</c>
    /// §۲۴-۱.
    /// </summary>
    public PersonTypes? PERSONTYPE { get; set; }

    public virtual ICollection<TB_ACCOUNT_LINK_TAFSILGROUP> TB_ACCOUNT_LINK_TAFSILGROUPs { get; set; } = new List<TB_ACCOUNT_LINK_TAFSILGROUP>();

    public virtual ICollection<TB_TAFSIL_LINK_TAFSILGROUP> TB_TAFSIL_LINK_TAFSILGROUPs { get; set; } = new List<TB_TAFSIL_LINK_TAFSILGROUP>();
}
