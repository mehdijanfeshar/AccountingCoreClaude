using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_PAYRECIVHEAD
{
    public Guid ID { get; set; }

    public string PAYRECIVCODE { get; set; } = null!;

    public string PAYRECIVDATE { get; set; } = null!;

    public string PAYRECIVDESCRIPTION { get; set; } = null!;

    public bool? PAYRECIVTYPE { get; set; }

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
