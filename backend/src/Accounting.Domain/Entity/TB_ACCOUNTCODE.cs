using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

public partial class TB_ACCOUNTCODE
{
    public Guid ID { get; set; }

    /// <summary>
    /// سطح سلسله‌مراتب کدینگ (گروه/کل/معین) — رجوع به <see cref="Accounting.Domain.ValueObjects.TypeCodes"/>
    /// برای مقادیر و مستندات کامل.
    /// </summary>
    public TypeCodes? TYPECODE { get; set; }

    public Guid? PARENTID { get; set; }

    public string? ACCCODE { get; set; }

    public string? ACCCODENAME { get; set; }

    /// <summary>
    /// ماهیت بدهکار/بستانکار حساب — رجوع به <see cref="Accounting.Domain.ValueObjects.TypeActivity"/>.
    /// ⚠️ کامنت ستون Oracle («(1بستانکار2بدهکار3بد-بس)نوع فعاليت») نادرست/کهنه است و مقادیر ۱ و
    /// ۲ را جابه‌جا گزارش می‌کند؛ برای مقدار واقعی و شواهد آن به مستندات enum مراجعه کن.
    /// </summary>
    public TypeActivity? TYPEACTIVITY { get; set; }

    public Guid? SOURCEANDCONSUME_ID { get; set; }

    public Guid? IDENTYGROUPS_ID { get; set; }

    /// <summary>
    /// موقت/دائم‌بودن حساب — رجوع به <see cref="Accounting.Domain.ValueObjects.TypeAccCode"/>.
    /// </summary>
    public TypeAccCode? TYPEACCCODE { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public bool? ISDELETED { get; set; }

    public string? MOINFORCLOSE { get; set; }

    /// <summary>
    /// رفتار موردانتظار هنگام ثبت سند خلاف ماهیت — رجوع به <see cref="Accounting.Domain.ValueObjects.TypeAction"/>.
    /// </summary>
    public TypeAction? TYPEACTION { get; set; }

    public virtual ICollection<TB_ACCOUNTCODE> InversePARENT { get; set; } = new List<TB_ACCOUNTCODE>();

    public virtual TB_ACCOUNTCODE? PARENT { get; set; }

    public virtual ICollection<TB_ACCOUNTCODE_INTERFACE> TB_ACCOUNTCODE_INTERFACEs { get; set; } = new List<TB_ACCOUNTCODE_INTERFACE>();

    public virtual ICollection<TB_ACCOUNTEXCEPTION> TB_ACCOUNTEXCEPTIONs { get; set; } = new List<TB_ACCOUNTEXCEPTION>();

    public virtual ICollection<TB_ACCOUNT_LINK_LEVEL> TB_ACCOUNT_LINK_LEVELs { get; set; } = new List<TB_ACCOUNT_LINK_LEVEL>();

    public virtual ICollection<TB_ACCOUNT_LINK_TAFSILGROUP> TB_ACCOUNT_LINK_TAFSILGROUPs { get; set; } = new List<TB_ACCOUNT_LINK_TAFSILGROUP>();

    public virtual ICollection<TB_ACCOUNT> TB_ACCOUNTs { get; set; } = new List<TB_ACCOUNT>();

    public virtual ICollection<TB_ATTRIBFORACCOUNTCODE> TB_ATTRIBFORACCOUNTCODEs { get; set; } = new List<TB_ATTRIBFORACCOUNTCODE>();

    public virtual ICollection<TB_ELAMDETAIL> TB_ELAMDETAILs { get; set; } = new List<TB_ELAMDETAIL>();

    public virtual ICollection<TB_EXPENCE> TB_EXPENCEs { get; set; } = new List<TB_EXPENCE>();

    public virtual ICollection<TB_PREDESCRIB> TB_PREDESCRIBs { get; set; } = new List<TB_PREDESCRIB>();

    public virtual ICollection<TB_RABET_CLOSING> TB_RABET_CLOSINGACCOUNTCODE_RABETs { get; set; } = new List<TB_RABET_CLOSING>();

    public virtual ICollection<TB_RABET_CLOSING> TB_RABET_CLOSINGACCOUNTCODEs { get; set; } = new List<TB_RABET_CLOSING>();

    public virtual ICollection<TB_RABET> TB_RABETs { get; set; } = new List<TB_RABET>();

    public virtual ICollection<TB_REVOLVING_FUND> TB_REVOLVING_FUNDs { get; set; } = new List<TB_REVOLVING_FUND>();

    public virtual ICollection<TB_VOUCHERSDETAIL> TB_VOUCHERSDETAILs { get; set; } = new List<TB_VOUCHERSDETAIL>();

    public virtual ICollection<TB_WHITEANDBLACKLIST> TB_WHITEANDBLACKLISTs { get; set; } = new List<TB_WHITEANDBLACKLIST>();

    public virtual ICollection<TB_WHITELIST> TB_WHITELISTs { get; set; } = new List<TB_WHITELIST>();

    public virtual ICollection<TB_WORKSHOP> TB_WORKSHOPs { get; set; } = new List<TB_WORKSHOP>();
}
