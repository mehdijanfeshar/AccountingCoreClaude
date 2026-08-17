using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_CHARGEANDCOST_DETAIL
{
    public string ID { get; set; } = null!;

    public string? CHARGEANDCOSTHEAD_ID { get; set; }

    public string? EXPENSE_ID { get; set; }

    public string? REVOLVINGFUND_ID { get; set; }

    public string? PAYTO { get; set; }

    public decimal? DEBTOR { get; set; }

    public decimal? CREDITOR { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public decimal? PAIDAMOUNT { get; set; }

    public virtual TB_CHARGEANDCOST_HEAD? CHARGEANDCOSTHEAD { get; set; }

    public virtual TB_EXPENCE? EXPENSE { get; set; }

    public virtual TB_REVOLVING_FUND? REVOLVINGFUND { get; set; }
}
