using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_IDENTITYFIXITEM
{
    /// <summary>
    /// آي دي مقادير ثابت شناسنامه 
    /// </summary>
    public string ID { get; set; } = null!;

    public string IDENTITYHEAD_ID { get; set; } = null!;

    /// <summary>
    /// آي دي زير گروه شناسنامه 
    /// </summary>
    public string IDENTITYSUBGRPS_ID { get; set; } = null!;

    /// <summary>
    /// مقدار شناسنامه 
    /// </summary>
    public string? FIXITEMS_VALUE { get; set; }

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
}
