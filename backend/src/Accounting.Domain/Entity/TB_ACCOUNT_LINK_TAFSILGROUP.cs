using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_ACCOUNT_LINK_TAFSILGROUP
{
    public Guid ID { get; set; }

    public Guid ACCOUNT_ID { get; set; }

    public Guid TAFSILGROUP_ID { get; set; }

    public Guid LEVEL_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_ACCOUNTCODE ACCOUNT { get; set; } = null!;

    public virtual TB_LEVEL_TAFSIL LEVEL { get; set; } = null!;

    public virtual TB_TAFSIL_GROUP TAFSILGROUP { get; set; } = null!;
}
