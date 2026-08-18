using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_ATTACH
{
    public Guid ID { get; set; }

    public string ATTACH_NAME { get; set; } = null!;

    public int ATTACH_SIZE { get; set; }

    public byte[]? ATTACH_FILE { get; set; }

    public byte ATTACH_RADIF { get; set; }

    public Guid? VOUCHERSHEAD_ID { get; set; }

    public Guid? TAFSILI_ID { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public string? TITLE { get; set; }

    public Guid? PAYRECEIVE_ID { get; set; }

    public virtual TB_TAFSILI? TAFSILI { get; set; }
}
