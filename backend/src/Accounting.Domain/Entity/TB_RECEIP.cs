using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_RECEIP
{
    public Guid ID { get; set; }

    public bool RECEIPT_KIND { get; set; }

    public string RECEIPT_DATE { get; set; } = null!;

    public string RECEIPT_NO { get; set; } = null!;

    public string? DATE_RSID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public virtual ICollection<TB_BANKCARTDETAIL> TB_BANKCARTDETAILs { get; set; } = new List<TB_BANKCARTDETAIL>();

    public virtual ICollection<TB_PAYRECIVDETAIL> TB_PAYRECIVDETAILs { get; set; } = new List<TB_PAYRECIVDETAIL>();

    public virtual ICollection<TB_VOUCHERSDETAIL> TB_VOUCHERSDETAILs { get; set; } = new List<TB_VOUCHERSDETAIL>();
}
