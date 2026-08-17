using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_CHARGEANDCOST_HEAD
{
    public string ID { get; set; } = null!;

    public bool CHARGEANDCOST_TYPE { get; set; }

    public string CHARGEANDCOST_CODE { get; set; } = null!;

    public string CHARGEANDCOST_DATE { get; set; } = null!;

    public string? DESCRIPTION { get; set; }

    public bool STATUS { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public string? ACCOUNT_ID { get; set; }

    public virtual TB_ACCOUNT? ACCOUNT { get; set; }

    public virtual ICollection<TB_CHARGEANDCOST_DETAIL> TB_CHARGEANDCOST_DETAILs { get; set; } = new List<TB_CHARGEANDCOST_DETAIL>();
}
