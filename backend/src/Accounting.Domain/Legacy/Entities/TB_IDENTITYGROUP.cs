using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_IDENTITYGROUP
{
    /// <summary>
    /// آي دي گروه اصلي شناسنامه 
    /// </summary>
    public string ID { get; set; } = null!;

    /// <summary>
    /// شرح گروه اصلي 
    /// </summary>
    public string IDENTITYGROUPS_DESC { get; set; } = null!;

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
    /// كاربر تغيير دهنده
    /// </summary>
    public string? CHANGEUSERID { get; set; }

    /// <summary>
    /// كد واحد 
    /// </summary>
    public string VAHEDCODE { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public string? IDENTITYGROUPS_CODE { get; set; }

    public string? TAFSILI_ID { get; set; }

    public virtual TB_TAFSILI? TAFSILI { get; set; }

    public virtual ICollection<TB_IDENTITYHEAD> TB_IDENTITYHEADs { get; set; } = new List<TB_IDENTITYHEAD>();

    public virtual ICollection<TB_IDENTITYSUBGRP> TB_IDENTITYSUBGRPs { get; set; } = new List<TB_IDENTITYSUBGRP>();
}
