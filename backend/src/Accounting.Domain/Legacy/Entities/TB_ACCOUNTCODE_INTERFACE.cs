using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_ACCOUNTCODE_INTERFACE
{
    public string ID { get; set; } = null!;

    public bool TYPE { get; set; }

    public string ACCOUNTCODEID { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_ACCOUNTCODE ACCOUNTCODE { get; set; } = null!;
}
