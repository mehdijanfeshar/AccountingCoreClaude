using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_VOUCHERSHEAD
{
    /// <summary>
    /// آي دي سند
    /// </summary>
    public Guid ID { get; set; }

    /// <summary>
    /// شماره واقعي سند
    /// </summary>
    public string? DOC_NUM { get; set; }

    /// <summary>
    /// تاريخ سند
    /// </summary>
    public string? DATE_DOC { get; set; }

    /// <summary>
    /// وضعيت سند
    /// </summary>
    public bool? DOCLIFE { get; set; }

    /// <summary>
    /// شرح سند
    /// </summary>
    public string? HEAD_DESC { get; set; }

    /// <summary>
    /// پيوست
    /// </summary>
    public string? APENDIX { get; set; }

    /// <summary>
    /// نوع سيستم4=اموال و3=اعلاميه مکانيزه
    /// </summary>
    public Guid? SYSTEM_TYPE { get; set; }

    /// <summary>
    /// سند آيا اختتاميه ميباشد
    /// </summary>
    public decimal? FLAG_STATE { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    /// <summary>
    /// كد واحد 
    /// </summary>
    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool? ISDELETED { get; set; }

    public byte[]? ATTACHFILE { get; set; }

    public string? ATTACHFILE_NAME { get; set; }

    public string? ATF_NUM { get; set; }

    /// <summary>
    /// 0دستي و 1 مکانيزه
    /// </summary>
    public bool? ISAUTOMATIC { get; set; }

    /// <summary>
    /// واحد گيرنده
    /// </summary>
    public string? SNDVAHEDCODE { get; set; }

    public Guid? PARENTHEAD_ID { get; set; }

    public string? GLOBALNUMBER { get; set; }

    public virtual TB_SYSTYPE? SYSTEM_TYPENavigation { get; set; }

    public virtual ICollection<TB_ELAMHEAD> TB_ELAMHEADs { get; set; } = new List<TB_ELAMHEAD>();

    public virtual ICollection<TB_PAYRECIVHEAD> TB_PAYRECIVHEADs { get; set; } = new List<TB_PAYRECIVHEAD>();

    public virtual ICollection<TB_TMP_VOUCHERHEAD> TB_TMP_VOUCHERHEADs { get; set; } = new List<TB_TMP_VOUCHERHEAD>();

    public virtual ICollection<TB_VOUCHERSDETAIL> TB_VOUCHERSDETAILs { get; set; } = new List<TB_VOUCHERSDETAIL>();
}
