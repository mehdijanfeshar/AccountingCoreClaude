using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_RABET_CLOSING
{
    public string ID { get; set; } = null!;

    public bool? TYPEACCOUNTCODE { get; set; }

    public string VAHEDTYPE_ID { get; set; } = null!;

    public string? ACCOUNTCODE_ID { get; set; }

    public string? ACCOUNTCODE_RABET_ID { get; set; }

    public string? TITLE { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public bool? ISDELETED { get; set; }

    public string? YEAR { get; set; }

    public virtual TB_ACCOUNTCODE? ACCOUNTCODE { get; set; }

    public virtual TB_ACCOUNTCODE? ACCOUNTCODE_RABET { get; set; }

    public virtual TB_VAHED_TYPE VAHEDTYPE { get; set; } = null!;
}
