using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_TAFSILI
{
    public string ID { get; set; } = null!;

    public string? TAFSILI_CODE { get; set; }

    public string? TAFSILI_NAME { get; set; }

    public bool? ISACTIVE { get; set; }

    public bool? PERSONTYPE { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public bool? ISDELETED { get; set; }

    public string? TAFSIL_DESC { get; set; }

    /// <summary>
    /// 2=setad 1=vahed
    /// </summary>
    public bool? OWNER { get; set; }

    public bool? VAHEDTYPE { get; set; }

    public virtual ICollection<TB_ACCOUNT_LINK_TAFSILI> TB_ACCOUNT_LINK_TAFSILIs { get; set; } = new List<TB_ACCOUNT_LINK_TAFSILI>();

    public virtual ICollection<TB_ATTACH> TB_ATTACHes { get; set; } = new List<TB_ATTACH>();

    public virtual ICollection<TB_ELAMDETAIL_LINK_TAFSILI> TB_ELAMDETAIL_LINK_TAFSILIs { get; set; } = new List<TB_ELAMDETAIL_LINK_TAFSILI>();

    public virtual ICollection<TB_EXPENCE_LINK_TAFSILI> TB_EXPENCE_LINK_TAFSILIs { get; set; } = new List<TB_EXPENCE_LINK_TAFSILI>();

    public virtual ICollection<TB_IDENTITYGROUP> TB_IDENTITYGROUPs { get; set; } = new List<TB_IDENTITYGROUP>();

    public virtual ICollection<TB_PAYRECIVDETAIL_LINK_TAFSILI> TB_PAYRECIVDETAIL_LINK_TAFSILIs { get; set; } = new List<TB_PAYRECIVDETAIL_LINK_TAFSILI>();

    public virtual ICollection<TB_REVOLVINGFUND_LINK_TAFSILI> TB_REVOLVINGFUND_LINK_TAFSILIs { get; set; } = new List<TB_REVOLVINGFUND_LINK_TAFSILI>();

    public virtual TB_VAHED_INFO? VAHEDCODENavigation { get; set; }
}
