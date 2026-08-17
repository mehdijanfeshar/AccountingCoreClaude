using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_CHECKBOOK
{
    public string ID { get; set; } = null!;

    public string ACCOUNT_ID { get; set; } = null!;

    public string? CHECKBOOK_TITLE { get; set; }

    public string CHECKBOOK_DATE { get; set; } = null!;

    public string FROMCHECKNUMBER { get; set; } = null!;

    public string TOCHECKNUMBER { get; set; } = null!;

    public string? CHECKTYPE_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public bool? CHECKBOOK_TYPE { get; set; }

    public string? SERIAL { get; set; }

    public virtual TB_ACCOUNT ACCOUNT { get; set; } = null!;

    public virtual TB_CHECK_TYPE? CHECKTYPE { get; set; }

    public virtual ICollection<TB_CHECK> TB_CHECKs { get; set; } = new List<TB_CHECK>();
}
