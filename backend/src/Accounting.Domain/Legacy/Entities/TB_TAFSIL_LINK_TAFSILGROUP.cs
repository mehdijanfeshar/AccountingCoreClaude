using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_TAFSIL_LINK_TAFSILGROUP
{
    public string ID { get; set; } = null!;

    public string TAFSIL_ID { get; set; } = null!;

    public string TAFSILGROUP_ID { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public bool ISDELETED { get; set; }

    public bool? VAHEDTYPE { get; set; }

    public virtual TB_TAFSIL_GROUP TAFSILGROUP { get; set; } = null!;
}
