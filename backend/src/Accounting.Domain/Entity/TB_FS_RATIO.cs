using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// نسبت مالی به‌صورت داده (ح-۸، سند منبع §۱۲-۳ «تحلیل و مقایسهٔ دوره‌ای»): صورت/مخرج با زبان فرمول قالب و فقط
/// <c>STMT(قالب, ردیف)</c>، روی مبلغ <b>نمایشی</b> (ماهیت بستانکار مثبت). مخرج خالی = خود صورت. FORMAT: ۱ درصد،
/// ۲ «برابر»، ۳ مبلغ. مالکیت مثل قواعد کنترل؛ برای یک کد، نسبت نزدیک‌ترین مالک جای مشترک را می‌گیرد. DDL 061.
/// </summary>
public partial class TB_FS_RATIO
{
    public Guid ID { get; set; }

    public string? VAHEDCODE { get; set; }

    public FsFramework FRAMEWORK { get; set; }

    public string CODE { get; set; } = null!;

    public string TITLE_FA { get; set; } = null!;

    public string NUMERATOR_EXPR { get; set; } = null!;

    public string? DENOMINATOR_EXPR { get; set; }

    public FsRatioFormat FORMAT { get; set; }

    public int ORDER_NO { get; set; }

    public bool IS_ACTIVE { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }
}
