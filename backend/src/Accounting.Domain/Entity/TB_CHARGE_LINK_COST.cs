using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_CHARGE_LINK_COST
{
    public Guid ID { get; set; }

    public Guid? CHARGE_ID { get; set; }

    public Guid? COST_ID { get; set; }

    public decimal? AMOUNT { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? YEAR { get; set; }

    public string? VAHEDCODE { get; set; }

    public bool ISDELETED { get; set; }
}
