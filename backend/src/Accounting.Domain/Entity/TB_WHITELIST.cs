using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_WHITELIST
{
    public Guid ID { get; set; }

    public Guid ACCOUNTCODE_ID { get; set; }

    public Guid? VAHEDTYPE_ID { get; set; }

    public Guid? VAHEDINFO_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool? ISDELETED { get; set; }

    public string? FROMAUTHORIZEDDATE { get; set; }

    public string? TOAUTHORIZEDDATE { get; set; }

    public string? FROMLIMITATIONDATE { get; set; }

    public string? TOLIMITATIONDATE { get; set; }

    public virtual TB_ACCOUNTCODE ACCOUNTCODE { get; set; } = null!;

    public virtual TB_VAHED_INFO? VAHEDINFO { get; set; }

    public virtual TB_VAHED_TYPE? VAHEDTYPE { get; set; }
}
