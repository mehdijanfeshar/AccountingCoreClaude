using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_WORKSHOP
{
    public Guid ID { get; set; }

    public Guid? BRANCH_ID { get; set; }

    public Guid ACCOUNTCODE_ID { get; set; }

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
