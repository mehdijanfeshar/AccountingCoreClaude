using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_WORKSHOP
{
    public string ID { get; set; } = null!;

    public string? BRANCH_ID { get; set; }

    public string ACCOUNTCODE_ID { get; set; } = null!;

    public string WORKSHOPNAME { get; set; } = null!;

    public string WORKSHOPCODE { get; set; } = null!;

    public DateTime? CREATEDDATE { get; set; }

    public byte[]? CHECKFILE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public bool? ISDELETED { get; set; }

    public bool ISACTIVE { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public virtual TB_ACCOUNTCODE ACCOUNTCODE { get; set; } = null!;

    public virtual TB_VAHED_INFO? BRANCH { get; set; }
}
