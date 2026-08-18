using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_PAYRECIVDETAIL_LINK_TAFSILI
{
    public Guid ID { get; set; }

    public Guid PAYRECIVDETAIL_ID { get; set; }

    public Guid TAFSILI_ID { get; set; }

    public Guid LEVEL_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public virtual TB_LEVEL_TAFSIL LEVEL { get; set; } = null!;

    public virtual TB_PAYRECIVDETAIL PAYRECIVDETAIL { get; set; } = null!;

    public virtual TB_TAFSILI TAFSILI { get; set; } = null!;
}
