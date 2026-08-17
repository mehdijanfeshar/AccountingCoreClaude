using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_CHEQUES_INCORRENT
{
    /// <summary>
    /// PK
    /// </summary>
    public string ID { get; set; } = null!;

    public string? CHECK_ID { get; set; }

    /// <summary>
    /// شماره واقعي سند
    /// </summary>
    public string DOC_NUM { get; set; } = null!;

    /// <summary>
    /// تاريخ سند
    /// </summary>
    public string DOC_DATE { get; set; } = null!;

    /// <summary>
    /// شماره پرداخت 
    /// </summary>
    public string CHEQ_NO { get; set; } = null!;

    /// <summary>
    /// تاريخ ايجاد 
    /// </summary>
    public string CHEQ_DATE { get; set; } = null!;

    /// <summary>
    /// بابت
    /// </summary>
    public string? PAPER_DESC { get; set; }

    /// <summary>
    /// دروجه
    /// </summary>
    public string? PAYTO { get; set; }

    /// <summary>
    /// تاريخ اجرا 
    /// </summary>
    public string? RECIVDATE { get; set; }

    /// <summary>
    /// شماره جاري 
    /// </summary>
    public string ACCOUNTNUMBER { get; set; } = null!;

    /// <summary>
    /// بستانكار
    /// </summary>
    public decimal CREDITOR { get; set; }

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

    public bool ISDELETED { get; set; }

    /// <summary>
    /// كد واحد
    /// </summary>
    public string VAHEDCODE { get; set; } = null!;

    /// <summary>
    /// سال استفاده از چك
    /// </summary>
    public string YEAR { get; set; } = null!;
}
