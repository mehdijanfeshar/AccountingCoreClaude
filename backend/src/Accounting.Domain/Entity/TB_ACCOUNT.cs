using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_ACCOUNT
{
    public Guid ID { get; set; }

    public string ACCOUNTNUMBER { get; set; } = null!;

    public string ACCOUNTHOLDER { get; set; } = null!;

    public string? CARDNUMBER { get; set; }

    public string? SHEBANUMBER { get; set; }

    public decimal? FIRSTAMOUNT { get; set; }

    public Guid? BANK_ID { get; set; }

    public Guid? BRANCH_ID { get; set; }

    public Guid? ACCOUNTTYPE_ID { get; set; }

    public Guid? ACCOUNTCODE_ID { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public byte[]? CHECKFILE { get; set; }

    public string? VAHEDCODE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public bool? ISDELETED { get; set; }

    public string? ACCOUNTOPENINGDATE { get; set; }

    public virtual TB_ACCOUNTCODE? ACCOUNTCODE { get; set; }

    public virtual TB_BANK_LIST? BANK { get; set; }

    public virtual TB_BANKBRANCH_LIST? BRANCH { get; set; }

    public virtual ICollection<TB_ACCOUNT_LINK_TAFSILI> TB_ACCOUNT_LINK_TAFSILIs { get; set; } = new List<TB_ACCOUNT_LINK_TAFSILI>();

    public virtual ICollection<TB_CHARGEANDCOST_HEAD> TB_CHARGEANDCOST_HEADs { get; set; } = new List<TB_CHARGEANDCOST_HEAD>();

    public virtual ICollection<TB_CHECKBOOK> TB_CHECKBOOKs { get; set; } = new List<TB_CHECKBOOK>();
}
