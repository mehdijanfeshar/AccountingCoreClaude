using System;
using System.Collections.Generic;

namespace Accounting.Domain.Entity;

public partial class TB_BANKCARTDETAIL
{
    /// <summary>
    /// آي دي كارت بانك 
    /// </summary>
    public Guid ID { get; set; }

    /// <summary>
    /// آي دي فيش يا حواله 
    /// </summary>
    public Guid? RECEIP_ID { get; set; }

    /// <summary>
    /// آي دي چك يا اعلاميه 
    /// </summary>
    public Guid? CHECK_ID { get; set; }

    /// <summary>
    /// كد بانك 
    /// </summary>
    public Guid? BANK_ID { get; set; }

    /// <summary>
    /// كد شعبه بانك 
    /// </summary>
    public Guid? BRANCH_ID { get; set; }

    /// <summary>
    /// شماره جاري 
    /// </summary>
    public string? ACCOUNTNUMBER { get; set; }

    /// <summary>
    /// ماه عملكرد 
    /// </summary>
    public string? MONTH { get; set; }

    /// <summary>
    /// شماره چك 
    /// </summary>
    public string? CHEQNO { get; set; }

    /// <summary>
    /// تاريخ اجرا 
    /// </summary>
    public string? RECIVDATE { get; set; }

    /// <summary>
    /// نوع مدرك بانكي (فيش يا حواله)   
    /// </summary>
    public bool? CHECKRECEIPTTYPE { get; set; }

    /// <summary>
    /// مبلغ بدهكاري 
    /// </summary>
    public decimal? DEBTOR { get; set; }

    /// <summary>
    /// مبلغ بستانكاري 
    /// </summary>
    public decimal? CREDITOR { get; set; }

    /// <summary>
    /// تاريخ ايجاد
    /// </summary>
    public DateTime? CREATEDDATE { get; set; }

    /// <summary>
    /// تاريخ تغيير
    /// </summary>
    public DateTime? UPDATEDDATE { get; set; }

    /// <summary>
    /// كاربر ايجاد كننده
    /// </summary>
    public string? ADDUSERID { get; set; }

    /// <summary>
    /// كاربر تغيير دهنده
    /// </summary>
    public string? CHANGEUSERID { get; set; }

    /// <summary>
    /// كد واحد
    /// </summary>
    public string? VAHEDCODE { get; set; }

    public bool? ISDELETED { get; set; }

    public string? YEAR { get; set; }

    /// <summary>
    /// آي دي در جريان سالهاي قبل  
    /// </summary>
    public Guid? CHECK_INCORRENT_ID { get; set; }

    public virtual TB_CHECK? CHECK { get; set; }

    public virtual TB_RECEIP? RECEIP { get; set; }
}
