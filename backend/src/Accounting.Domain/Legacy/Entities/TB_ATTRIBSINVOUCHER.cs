using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_ATTRIBSINVOUCHER
{
    /// <summary>
    /// اي دي كدهاي شناسه دار 
    /// </summary>
    public string ID { get; set; } = null!;

    /// <summary>
    /// آي دي ريز اسناد 
    /// </summary>
    public string VOUCHERSDETAIL_ID { get; set; } = null!;

    public string ATTRIBFORACCOUNTCODE_ID { get; set; } = null!;

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
