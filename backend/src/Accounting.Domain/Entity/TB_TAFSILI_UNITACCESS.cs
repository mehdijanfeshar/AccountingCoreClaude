using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_TAFSILI_UNITACCESS
{
    public Guid ID { get; set; }

    public Guid TAFSILI_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public string? VAHEDCODE { get; set; }
}
