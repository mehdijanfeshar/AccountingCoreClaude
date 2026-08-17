using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_PAYRECIVDETAIL_LINK_TAFSILI
{
    public string ID { get; set; } = null!;

    public string PAYRECIVDETAIL_ID { get; set; } = null!;

    public string TAFSILI_ID { get; set; } = null!;

    public string LEVEL_ID { get; set; } = null!;

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
