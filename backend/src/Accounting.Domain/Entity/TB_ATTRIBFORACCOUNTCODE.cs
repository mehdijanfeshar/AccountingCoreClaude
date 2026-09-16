using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

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
    /// مشخصه تعداد شناسه — تعداد/شمارهٔ خانهٔ صفت (عدد صحیح صرف، NOT enum). تا فاز ۲۷ (بچ ۲)
    /// به‌اشتباه <c>bool</c> بود؛ در پروژهٔ مرجع <c>int AttribBoxNo</c> است. رجوع به
    /// <c>docs/centralaccount-business-reference.md</c> §۲۴-۱ ("بدترین مورد این پاس — اصلاً enum
    /// نیست، عدد است"). <c>short</c> انتخاب شد نه <c>int</c>، مطابق ستون فیزیکی Oracle
    /// <c>NUMBER(1)</c> (هم‌الگوی <c>TB_TAFSIL_LINK_TAFSILGROUP.VAHEDTYPE</c>).
    /// </summary>
    public short ATTRIBBOXNO { get; set; }

    /// <summary>
    /// مشخصه نوع شناسه — <see cref="ValueObjects.AttribFlag"/> (۱=عدد, ۲=تاریخ). تا فاز ۲۷ (بچ ۲)
    /// به‌اشتباه <c>bool</c> بود؛ رجوع به <c>docs/centralaccount-business-reference.md</c> §۲۴-۱.
    /// </summary>
    public AttribFlag FLAG { get; set; }

    /// <summary>
    /// مشخصه طول شناسه
    /// </summary>
    public byte LENATR { get; set; }

    /// <summary>
    /// جمع پذير يا جمع ناپذير — <see cref="ValueObjects.AttribSum"/> (۱=جمع‌پذیر, ۲=جمع‌ناپذیر).
    /// تا فاز ۲۷ (بچ ۲) به‌اشتباه <c>bool</c> بود؛ رجوع به
    /// <c>docs/centralaccount-business-reference.md</c> §۲۴-۱.
    /// </summary>
    public AttribSum ATTRIBSUM { get; set; }

    /// <summary>
    /// <see cref="ValueObjects.AttribControl"/> (۱=غیرصفر, ۲=تاریخ). تا فاز ۲۷ (بچ ۲) به‌اشتباه
    /// <c>bool?</c> بود؛ رجوع به <c>docs/centralaccount-business-reference.md</c> §۲۴-۱. مپینگ
    /// Fluent این ستون همچنان تناقض پیشین <c>.IsRequired()</c> + <c>HasDefaultValueSql("null ")</c>
    /// را دارد — این فاز فقط نوع CLR را اصلاح کرد، نه آن تناقض را.
    /// </summary>
    public AttribControl? CONTROLID { get; set; }

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
