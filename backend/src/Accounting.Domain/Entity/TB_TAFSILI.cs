using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

public partial class TB_TAFSILI
{
    public Guid ID { get; set; }

    public string? TAFSILI_CODE { get; set; }

    public string? TAFSILI_NAME { get; set; }

    /// <summary>
    /// فعال/غیرفعال — <see cref="ValueObjects.TafsiliActiveState"/> (۱=فعال, ۲=غیرفعال). تا فاز ۲۷
    /// (بچ ۱) به‌اشتباه <c>bool?</c> بود؛ رجوع به <c>docs/centralaccount-business-reference.md</c>
    /// §۲۴-۱.
    /// </summary>
    public TafsiliActiveState? ISACTIVE { get; set; }

    /// <summary>
    /// نوع شخص — <see cref="ValueObjects.PersonTypes"/> (۱=حقیقی, ۲=حقوقی, ۳=سایر). تا فاز ۲۷
    /// (بچ ۱) به‌اشتباه <c>bool?</c> بود؛ رجوع به <c>docs/centralaccount-business-reference.md</c>
    /// §۲۴-۱.
    /// </summary>
    public PersonTypes? PERSONTYPE { get; set; }

    public DateTime? CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string? ADDUSERID { get; set; }

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public bool? ISDELETED { get; set; }

    public string? TAFSIL_DESC { get; set; }

    /// <summary>
    /// مالکیت — <see cref="ValueObjects.Owners"/> (۱=سراسری, ۲=داخلی). کامنت قدیمی این ستون در
    /// Oracle («2=setad 1=vahed») نادرست/کهنه است و مقادیر را جابه‌جا گزارش می‌کند — رجوع به XML
    /// doc خودِ <see cref="ValueObjects.Owners"/>. تا فاز ۲۷ (بچ ۱) به‌اشتباه <c>bool?</c> بود.
    /// </summary>
    public Owners? OWNER { get; set; }

    /// <summary>
    /// دستهٔ واحد (بیمه/درمان/همه) — بازاستفاده از <see cref="ValueObjects.VahedCategory"/> (همان
    /// enum <c>TypeKoli</c> پروژهٔ مرجع که برای <c>TB_TAFSIL_LINK_TAFSILGROUP.VAHEDTYPE</c> هم
    /// استفاده می‌شود). ⚠️ **شاهد قوی، نه اثبات‌شده روی دادهٔ ما** — رجوع به XML doc
    /// <see cref="ValueObjects.VahedCategory"/>. تا فاز ۲۷ (بچ ۱) به‌اشتباه <c>bool?</c> بود.
    /// </summary>
    public VahedCategory? VAHEDTYPE { get; set; }

    public virtual ICollection<TB_ACCOUNT_LINK_TAFSILI> TB_ACCOUNT_LINK_TAFSILIs { get; set; } = new List<TB_ACCOUNT_LINK_TAFSILI>();

    public virtual ICollection<TB_ATTACH> TB_ATTACHes { get; set; } = new List<TB_ATTACH>();

    public virtual ICollection<TB_ELAMDETAIL_LINK_TAFSILI> TB_ELAMDETAIL_LINK_TAFSILIs { get; set; } = new List<TB_ELAMDETAIL_LINK_TAFSILI>();

    public virtual ICollection<TB_EXPENCE_LINK_TAFSILI> TB_EXPENCE_LINK_TAFSILIs { get; set; } = new List<TB_EXPENCE_LINK_TAFSILI>();

    public virtual ICollection<TB_IDENTITYGROUP> TB_IDENTITYGROUPs { get; set; } = new List<TB_IDENTITYGROUP>();

    public virtual ICollection<TB_PAYRECIVDETAIL_LINK_TAFSILI> TB_PAYRECIVDETAIL_LINK_TAFSILIs { get; set; } = new List<TB_PAYRECIVDETAIL_LINK_TAFSILI>();

    public virtual ICollection<TB_REVOLVINGFUND_LINK_TAFSILI> TB_REVOLVINGFUND_LINK_TAFSILIs { get; set; } = new List<TB_REVOLVINGFUND_LINK_TAFSILI>();

    public virtual TB_VAHED_INFO? VAHEDCODENavigation { get; set; }
}
