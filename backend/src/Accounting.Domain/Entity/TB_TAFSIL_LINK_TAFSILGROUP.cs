using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_TAFSIL_LINK_TAFSILGROUP
{
    public Guid ID { get; set; }

    public Guid TAFSIL_ID { get; set; }

    public Guid TAFSILGROUP_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public bool ISDELETED { get; set; }

    public bool? VAHEDTYPE { get; set; }

    public virtual TB_TAFSIL_GROUP TAFSILGROUP { get; set; } = null!;
}
