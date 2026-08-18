using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_TMP_VOUCHERHEAD
{
    public Guid ID { get; set; }

    public Guid? VOUCHERSHEAD_ID { get; set; }

    public string? DATE_DOC { get; set; }

    public string? HEAD_DESC { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool? ISDELETED { get; set; }

    public string? SYS_TYPE { get; set; }

    public Guid? SOURCEID { get; set; }

    public virtual ICollection<TB_TMP_VOUCHERSDETAIL> TB_TMP_VOUCHERSDETAILs { get; set; } = new List<TB_TMP_VOUCHERSDETAIL>();

    public virtual TB_VOUCHERSHEAD? VOUCHERSHEAD { get; set; }
}
