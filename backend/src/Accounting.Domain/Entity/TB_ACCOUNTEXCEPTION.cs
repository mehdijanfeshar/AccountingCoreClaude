using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_ACCOUNTEXCEPTION
{
    public Guid ID { get; set; }

    public Guid ACCOUNTCOE_ID { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public Guid VAHEDTYPE_ID { get; set; }

    public virtual TB_ACCOUNTCODE ACCOUNTCOE { get; set; } = null!;

    public virtual TB_VAHED_TYPE VAHEDTYPE { get; set; } = null!;
}
