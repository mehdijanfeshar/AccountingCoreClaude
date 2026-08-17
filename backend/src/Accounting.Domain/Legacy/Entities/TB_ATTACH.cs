using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_ATTACH
{
    public string ID { get; set; } = null!;

    public string ATTACH_NAME { get; set; } = null!;

    public int ATTACH_SIZE { get; set; }

    public byte[]? ATTACH_FILE { get; set; }

    public byte ATTACH_RADIF { get; set; }

    public string? VOUCHERSHEAD_ID { get; set; }

    public string? TAFSILI_ID { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public string? TITLE { get; set; }

    public string? PAYRECEIVE_ID { get; set; }

    public virtual TB_TAFSILI? TAFSILI { get; set; }
}
