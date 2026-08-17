using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_RABET
{
    public string ID { get; set; } = null!;

    public string? RABETTYPE_ID { get; set; }

    public string? ACCOUNTCODE_ID { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public bool? ISDELETED { get; set; }

    public virtual TB_ACCOUNTCODE? ACCOUNTCODE { get; set; }

    public virtual TB_RABET_TYPE? RABETTYPE { get; set; }
}
