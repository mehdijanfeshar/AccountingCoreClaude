using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_ACCOUNT_LINK_LEVEL
{
    public string ID { get; set; } = null!;

    public string ACCOUNT_ID { get; set; } = null!;

    public string LEVEL_ID { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_ACCOUNTCODE ACCOUNT { get; set; } = null!;

    public virtual TB_LEVEL_TAFSIL LEVEL { get; set; } = null!;
}
