using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_REVOLVING_FUND
{
    public Guid ID { get; set; }

    public string CODE { get; set; } = null!;

    public string NAME { get; set; } = null!;

    public string? DESCRIPTION { get; set; }

    public decimal? DEFAULTAMOUNT { get; set; }

    public Guid? ACCOUNTCODE_ID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }

    public string? CHANGEUSERID { get; set; }

    public bool? ISDELETED { get; set; }

    public virtual TB_ACCOUNTCODE? ACCOUNTCODE { get; set; }

    public virtual ICollection<TB_CHARGEANDCOST_DETAIL> TB_CHARGEANDCOST_DETAILs { get; set; } = new List<TB_CHARGEANDCOST_DETAIL>();

    public virtual ICollection<TB_REVOLVINGFUND_LINK_TAFSILI> TB_REVOLVINGFUND_LINK_TAFSILIs { get; set; } = new List<TB_REVOLVINGFUND_LINK_TAFSILI>();
}
