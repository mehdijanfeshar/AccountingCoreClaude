using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_ATTRIBFORACCOUNTCODE
{
    /// <summary>
    /// اي دي كدهاي شناسه دار 
    /// </summary>
    public Guid ID { get; set; }

    /// <summary>
    /// اي دي كدينگ مالي 
    /// </summary>
    public Guid ACCOUNTCODE_ID { get; set; }

    /// <summary>
    /// مشخصه تعداد شناسه 
    /// </summary>
    public bool ATTRIBBOXNO { get; set; }

    /// <summary>
    /// مشخصه نوع شناسه 
    /// </summary>
    public bool FLAG { get; set; }

    /// <summary>
    /// مشخصه طول شناسه 
    /// </summary>
    public byte LENATR { get; set; }

    /// <summary>
    /// جمع پذير يا جمع ناپذير 
    /// </summary>
    public bool ATTRIBSUM { get; set; }

    public bool? CONTROLID { get; set; }

    /// <summary>
    /// كد واحد 
    /// </summary>
    public string VAHEDCODE { get; set; } = null!;

    public string YEAR { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_ACCOUNTCODE ACCOUNTCODE { get; set; } = null!;

    public virtual ICollection<TB_ATTRIBSINVOUCHER> TB_ATTRIBSINVOUCHERs { get; set; } = new List<TB_ATTRIBSINVOUCHER>();
}
