using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_IDENTITYSUBGRP
{
    /// <summary>
    /// آي دي زير گروه شناسنامه 
    /// </summary>
    public Guid ID { get; set; }

    /// <summary>
    /// آي دي گروه اصلي شناسنامه 
    /// </summary>
    public Guid IDENTYGROUPS_ID { get; set; }

    /// <summary>
    /// شرح
    /// </summary>
    public string SUBGRPS_DESC { get; set; } = null!;

    /// <summary>
    /// طول 
    /// </summary>
    public byte SUBGRPS_LEN { get; set; }

    /// <summary>
    /// جمع پذير يا ناپذير بودن 
    /// </summary>
    public bool SUMFLAG { get; set; }

    /// <summary>
    /// ثابت يا متغير بودن 
    /// </summary>
    public bool FIXED { get; set; }

    /// <summary>
    /// نوع : حروف, اعداد, يا هردو 
    /// </summary>
    public bool? SUBGRPS_TYPE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    /// <summary>
    /// كد واحد 
    /// </summary>
    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public string? IDENTYSUBGROUPS_CODE { get; set; }

    public virtual TB_IDENTITYGROUP IDENTYGROUPS { get; set; } = null!;

    public virtual ICollection<TB_IDENTITYDETAIL> TB_IDENTITYDETAILs { get; set; } = new List<TB_IDENTITYDETAIL>();

    public virtual ICollection<TB_IDENTITYFIXITEM> TB_IDENTITYFIXITEMs { get; set; } = new List<TB_IDENTITYFIXITEM>();
}
