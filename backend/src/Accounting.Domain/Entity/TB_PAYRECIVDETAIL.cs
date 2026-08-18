using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_PAYRECIVDETAIL
{
    public Guid ID { get; set; }

    public Guid? ACCOUNTCODE_ID { get; set; }

    /// <summary>
    /// شماره فيش
    /// </summary>
    public Guid? RECEIPT_ID { get; set; }

    public Guid? CHECK_ID { get; set; }

    public string? ARTICLEDESCRIPTION { get; set; }

    public int RADIF { get; set; }

    public decimal DEBTOR { get; set; }

    public decimal CREDITOR { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public string YEAR { get; set; } = null!;

    public Guid PAYRECIVHEAD_ID { get; set; }

    public virtual TB_PAYRECIVHEAD PAYRECIVHEAD { get; set; } = null!;

    public virtual TB_RECEIP? RECEIPT { get; set; }

    public virtual ICollection<TB_PAYRECIVDETAIL_LINK_TAFSILI> TB_PAYRECIVDETAIL_LINK_TAFSILIs { get; set; } = new List<TB_PAYRECIVDETAIL_LINK_TAFSILI>();
}
