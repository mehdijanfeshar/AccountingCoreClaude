using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_RABET
{
    public Guid ID { get; set; }

    public Guid? RABETTYPE_ID { get; set; }

    public Guid? ACCOUNTCODE_ID { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public bool? ISDELETED { get; set; }

    public virtual TB_ACCOUNTCODE? ACCOUNTCODE { get; set; }

    public virtual TB_RABET_TYPE? RABETTYPE { get; set; }
}
