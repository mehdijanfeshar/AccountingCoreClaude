using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_WHITEANDBLACKLIST
{
    public string ID { get; set; } = null!;

    public string ACCOUNTCODE_ID { get; set; } = null!;

    public string? VAHEDTYPE_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool? ISDELETED { get; set; }

    public string? FROMAUTHORIZEDDATE { get; set; }

    public string? TOAUTHORIZEDDATE { get; set; }

    public string? FROMLIMITATIONDATE { get; set; }

    public string? TOLIMITATIONDATE { get; set; }

    public bool? STATE { get; set; }

    public virtual TB_ACCOUNTCODE ACCOUNTCODE { get; set; } = null!;

    public virtual TB_VAHED_TYPE? VAHEDTYPE { get; set; }
}
