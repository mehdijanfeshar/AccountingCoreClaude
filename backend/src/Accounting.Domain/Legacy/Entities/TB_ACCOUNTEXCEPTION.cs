using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_ACCOUNTEXCEPTION
{
    public string ID { get; set; } = null!;

    public string ACCOUNTCOE_ID { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public string VAHEDTYPE_ID { get; set; } = null!;

    public virtual TB_ACCOUNTCODE ACCOUNTCOE { get; set; } = null!;

    public virtual TB_VAHED_TYPE VAHEDTYPE { get; set; } = null!;
}
