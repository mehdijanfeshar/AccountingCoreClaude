using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_BILL_LOG
{
    public Guid ID { get; set; }

    public string? LOG_DESC { get; set; }

    public string? LOG_DATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }
}
