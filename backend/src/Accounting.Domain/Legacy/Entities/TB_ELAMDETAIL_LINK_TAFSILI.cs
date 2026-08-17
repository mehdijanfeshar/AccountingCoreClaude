using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_ELAMDETAIL_LINK_TAFSILI
{
    public string ID { get; set; } = null!;

    public string? ELAMDETAIL_ID { get; set; }

    public string? TAFSILI_ID { get; set; }

    public string? LEVEL_ID { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public bool? ISDELETED { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public virtual TB_ELAMDETAIL? ELAMDETAIL { get; set; }

    public virtual TB_LEVEL_TAFSIL? LEVEL { get; set; }

    public virtual TB_TAFSILI? TAFSILI { get; set; }
}
