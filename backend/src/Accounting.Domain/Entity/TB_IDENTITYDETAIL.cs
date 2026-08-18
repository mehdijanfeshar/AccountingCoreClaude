using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_IDENTITYDETAIL
{
    /// <summary>
    /// آي دي مقادير متغير شناسنامه 
    /// </summary>
    public Guid ID { get; set; }

    /// <summary>
    /// آي دي زير گروه شناسنامه 
    /// </summary>
    public Guid IDENTITYSUBGRPS_ID { get; set; }

    public Guid IDENTITYHEAD_ID { get; set; }

    /// <summary>
    /// آي دي آرتيكل اسناد 
    /// </summary>
    public Guid VOUCHERSDETAIL_ID { get; set; }

    /// <summary>
    /// مقدار شناسنامه 
    /// </summary>
    public string? DETAIL_VALUE { get; set; }

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

    public virtual TB_IDENTITYHEAD IDENTITYHEAD { get; set; } = null!;

    public virtual TB_IDENTITYSUBGRP IDENTITYSUBGRPS { get; set; } = null!;

    public virtual TB_VOUCHERSDETAIL VOUCHERSDETAIL { get; set; } = null!;
}
