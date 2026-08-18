using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_ACCOUNTCODE_INTERFACE
{
    public Guid ID { get; set; }

    public bool TYPE { get; set; }

    public Guid ACCOUNTCODEID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_ACCOUNTCODE ACCOUNTCODE { get; set; } = null!;
}
