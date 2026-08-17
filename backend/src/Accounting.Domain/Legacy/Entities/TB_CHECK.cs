using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_CHECK
{
    /// <summary>
    ///  آي دي پرداختني (چك يا اعلاميه
    /// </summary>
    public string ID { get; set; } = null!;

    /// <summary>
    /// آي دي دسته چك 
    /// </summary>
    public string CHECKBOOK_ID { get; set; } = null!;

    /// <summary>
    /// شماره پرداخت 
    /// </summary>
    public string CHEQ_NO { get; set; } = null!;

    /// <summary>
    /// تاريخ ايجاد 
    /// </summary>
    public string? CHEQ_DATE { get; set; }

    /// <summary>
    /// بابت
    /// </summary>
    public string? PAPER_DESC { get; set; }

    /// <summary>
    /// تاريخ اجرا 
    /// </summary>
    public string? DATE_RSID { get; set; }

    /// <summary>
    /// ابطال 
    /// </summary>
    public bool EBTAL { get; set; }

    /// <summary>
    /// دروجه
    /// </summary>
    public string? PAYTO { get; set; }

    /// <summary>
    /// چاپ
    /// </summary>
    public bool PRINT { get; set; }

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

    /// <summary>
    /// كد واحد 
    /// </summary>
    public string VAHEDCODE { get; set; } = null!;

    public bool ISDELETED { get; set; }

    public virtual TB_CHECKBOOK CHECKBOOK { get; set; } = null!;

    public virtual ICollection<TB_BANKCARTDETAIL> TB_BANKCARTDETAILs { get; set; } = new List<TB_BANKCARTDETAIL>();
}
