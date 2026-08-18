using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_ELAMDETAIL
{
    /// <summary>
    /// آي دي ريز اعلاميه 
    /// </summary>
    public Guid ID { get; set; }

    /// <summary>
    /// آي دي حساب تفضيلي
    /// </summary>
    public Guid? ACCOUNTCODE_ID { get; set; }

    /// <summary>
    /// آي دي تيتر اعلاميه 
    /// </summary>
    public Guid? ELAMHEAD_ID { get; set; }

    /// <summary>
    /// بدهكار
    /// </summary>
    public decimal? DEBTOR { get; set; }

    /// <summary>
    /// بستانكار
    /// </summary>
    public decimal? CREDITOR { get; set; }

    /// <summary>
    /// شرح آرتيكل اعلاميه
    /// </summary>
    public string? ELAMD_DESC { get; set; }

    /// <summary>
    /// شماره شناسايي 
    /// </summary>
    public string? ELAM_ATRIBNO { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public bool? ISDELETED { get; set; }

    /// <summary>
    /// كد واحد 
    /// </summary>
    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public virtual TB_ACCOUNTCODE? ACCOUNTCODE { get; set; }

    public virtual TB_ELAMHEAD? ELAMHEAD { get; set; }

    public virtual ICollection<TB_ELAMDETAIL_LINK_TAFSILI> TB_ELAMDETAIL_LINK_TAFSILIs { get; set; } = new List<TB_ELAMDETAIL_LINK_TAFSILI>();
}
