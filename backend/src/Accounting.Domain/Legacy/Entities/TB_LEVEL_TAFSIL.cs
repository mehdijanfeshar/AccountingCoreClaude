using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_LEVEL_TAFSIL
{
    public string ID { get; set; } = null!;

    public string LEVEL_CODE { get; set; } = null!;

    public string LEVEL_NAME { get; set; } = null!;

    /// <summary>
    /// تاريخ ايجاد
    /// </summary>
    public DateTime CREATEDDATE { get; set; }

    /// <summary>
    /// تاريخ تغيير
    /// </summary>
    public DateTime? UPDATEDDATE { get; set; }

    /// <summary>
    /// كاربر ايجاد كننده
    /// </summary>
    public string ADDUSERID { get; set; } = null!;

    /// <summary>
    ///  کاربر تغيير دهنده
    /// </summary>
    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public virtual ICollection<TB_ACCOUNT_LINK_LEVEL> TB_ACCOUNT_LINK_LEVELs { get; set; } = new List<TB_ACCOUNT_LINK_LEVEL>();

    public virtual ICollection<TB_ACCOUNT_LINK_TAFSILGROUP> TB_ACCOUNT_LINK_TAFSILGROUPs { get; set; } = new List<TB_ACCOUNT_LINK_TAFSILGROUP>();

    public virtual ICollection<TB_ACCOUNT_LINK_TAFSILI> TB_ACCOUNT_LINK_TAFSILIs { get; set; } = new List<TB_ACCOUNT_LINK_TAFSILI>();

    public virtual ICollection<TB_ELAMDETAIL_LINK_TAFSILI> TB_ELAMDETAIL_LINK_TAFSILIs { get; set; } = new List<TB_ELAMDETAIL_LINK_TAFSILI>();

    public virtual ICollection<TB_EXPENCE_LINK_TAFSILI> TB_EXPENCE_LINK_TAFSILIs { get; set; } = new List<TB_EXPENCE_LINK_TAFSILI>();

    public virtual ICollection<TB_PAYRECIVDETAIL_LINK_TAFSILI> TB_PAYRECIVDETAIL_LINK_TAFSILIs { get; set; } = new List<TB_PAYRECIVDETAIL_LINK_TAFSILI>();

    public virtual ICollection<TB_REVOLVINGFUND_LINK_TAFSILI> TB_REVOLVINGFUND_LINK_TAFSILIs { get; set; } = new List<TB_REVOLVINGFUND_LINK_TAFSILI>();
}
