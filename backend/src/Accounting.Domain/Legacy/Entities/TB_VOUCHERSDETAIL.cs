using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_VOUCHERSDETAIL
{
    public string ID { get; set; } = null!;

    public string? ACCOUNT_ID { get; set; }

    public string? RECEIP_ID { get; set; }

    public string? CHECK_ID { get; set; }

    public string? LOWLEVELCODE_ID { get; set; }

    public string? ETEBAR_ID { get; set; }

    public string? DESCRIPTION { get; set; }

    public int? RADIF { get; set; }

    public decimal? DEBTOR { get; set; }

    public decimal? CREDITOR { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public bool? ISDELETED { get; set; }

    public string? VOUCHERSHEAD_ID { get; set; }

    public string? YEAR { get; set; }

    public virtual TB_ACCOUNTCODE? ACCOUNT { get; set; }

    public virtual TB_RECEIP? RECEIP { get; set; }

    public virtual ICollection<TB_ATTRIBSINVOUCHER> TB_ATTRIBSINVOUCHERs { get; set; } = new List<TB_ATTRIBSINVOUCHER>();

    public virtual ICollection<TB_IDENTITYDETAIL> TB_IDENTITYDETAILs { get; set; } = new List<TB_IDENTITYDETAIL>();

    public virtual ICollection<TB_VOUCHERDETAIL_LINK_TAFSILI> TB_VOUCHERDETAIL_LINK_TAFSILIs { get; set; } = new List<TB_VOUCHERDETAIL_LINK_TAFSILI>();

    public virtual TB_VOUCHERSHEAD? VOUCHERSHEAD { get; set; }
}
