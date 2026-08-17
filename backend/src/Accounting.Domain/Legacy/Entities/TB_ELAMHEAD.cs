using System;
using System.Collections.Generic;

namespace Accounting.Domain.Legacy;

public partial class TB_ELAMHEAD
{
    /// <summary>
    /// آي دي تيتر اعلاميه 
    /// </summary>
    public string ID { get; set; } = null!;

    /// <summary>
    /// آي دي سند
    /// </summary>
    public string? VOUCHERSHEAD_ID { get; set; }

    /// <summary>
    /// شماره سريال اعلاميه  
    /// </summary>
    public string? ELAMH_SERIALNO { get; set; }

    public string? ELAMH_CODE { get; set; }

    /// <summary>
    /// شماره دبيرخانه 
    /// </summary>
    public string? ELAMH_DABIRNO { get; set; }

    /// <summary>
    /// تاريخ دبيرخانه
    /// </summary>
    public string? ELAMH_DABIRDATE { get; set; }

    /// <summary>
    /// تعداد چاپ 
    /// </summary>
    public short? ELAMH_PRINTNO { get; set; }

    /// <summary>
    /// نوع اعلاميه 1بد     2بس
    /// </summary>
    public bool? ELAMH_CASE { get; set; }

    /// <summary>
    /// سريال اعلاميه ورودي
    /// </summary>
    public string? SERIALNO_INPUT { get; set; }

    /// <summary>
    /// ارسال از طريق وب=1-ارسال شده=2
    /// </summary>
    public byte? WEB_STAT { get; set; }

    /// <summary>
    /// تاريخ اعلاميه
    /// </summary>
    public string? ELAMH_DATE { get; set; }

    /// <summary>
    /// شرح اعلاميه
    /// </summary>
    public string? ELAMH_DESC { get; set; }

    /// <summary>
    /// کارگاه
    /// </summary>
    public string? WORKSHOP_ID { get; set; }

    /// <summary>
    /// ،شماره بدهي ، شماره رسيد ليست
    /// </summary>
    public string? ELAMH_RCVNO { get; set; }

    /// <summary>
    /// ،تاريخ پيمان، تاريخ رسيد ليست
    /// </summary>
    public string? ELAMH_RCVDT { get; set; }

    /// <summary>
    /// ماه عملکرد ليست
    /// </summary>
    public string? ELAMH_LSTMON { get; set; }

    /// <summary>
    /// برگه پرداخت
    /// </summary>
    public string? PAY_NO { get; set; }

    /// <summary>
    ///  3حق بيمه نوع اعلاميه 1ذي حسابي 2سايردرآمد
    /// </summary>
    public bool? ELAMHDRAMAD_TYPE { get; set; }

    /// <summary>
    /// شماره پيمان
    /// </summary>
    public string? PEIMAN_NO { get; set; }

    /// <summary>
    /// کدکارگاه
    /// </summary>
    public string? ELAMH_WORKSHOPCODE { get; set; }

    /// <summary>
    /// نام کارگاه
    /// </summary>
    public string? ELAMH_WORKSHOPNAME { get; set; }

    /// <summary>
    /// كدواحد گيرنده يا ارسال كننده 
    /// </summary>
    public string? ELAMH_SENDRCVVAHED { get; set; }

    public string? ELAMH_YEAR { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public bool? ISDELETED { get; set; }

    /// <summary>
    /// كد واحد 
    /// </summary>
    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    /// <summary>
    /// شناسه اعلاميه صادره
    /// </summary>
    public string? ELAMSENDERID { get; set; }

    public virtual ICollection<TB_ELAMDETAIL> TB_ELAMDETAILs { get; set; } = new List<TB_ELAMDETAIL>();

    public virtual TB_VOUCHERSHEAD? VOUCHERSHEAD { get; set; }
}
