using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_ATTRIBSINVOUCHER
{
    /// <summary>
    /// اي دي كدهاي شناسه دار 
    /// </summary>
    public Guid ID { get; set; }

    /// <summary>
    /// آي دي ريز اسناد 
    /// </summary>
    public Guid VOUCHERSDETAIL_ID { get; set; }

    public Guid ATTRIBFORACCOUNTCODE_ID { get; set; }

    /// <summary>
    /// مقدار شناسه 
    /// </summary>
    public string? ATTRIBUTEVALUE { get; set; }

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

    public virtual TB_ATTRIBFORACCOUNTCODE ATTRIBFORACCOUNTCODE { get; set; } = null!;

    public virtual TB_VOUCHERSDETAIL VOUCHERSDETAIL { get; set; } = null!;
}
