using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_IDENTITYHEAD
{
    public Guid ID { get; set; }

    public Guid IDENTITYGROUPS_ID { get; set; }

    /// <summary>
    /// سريال هر موضوع شناسنامه
    /// </summary>
    public int SERIAL { get; set; }

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

    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public virtual TB_IDENTITYGROUP IDENTITYGROUPS { get; set; } = null!;

    public virtual ICollection<TB_IDENTITYDETAIL> TB_IDENTITYDETAILs { get; set; } = new List<TB_IDENTITYDETAIL>();

    public virtual ICollection<TB_IDENTITYFIXITEM> TB_IDENTITYFIXITEMs { get; set; } = new List<TB_IDENTITYFIXITEM>();
}
