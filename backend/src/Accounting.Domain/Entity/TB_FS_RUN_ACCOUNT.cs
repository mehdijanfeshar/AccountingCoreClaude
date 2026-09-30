using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// سهم یک معین در یک ردیف «حساب» اجرا، به تفکیک زیرواحد سطح اول اجرا — سطح «حساب» و «واحد»
/// Drill-down (بخش ۴۵-ج). فاز ۴۵-ب.
/// </summary>
public partial class TB_FS_RUN_ACCOUNT
{
    public Guid ID { get; set; }

    public Guid RUN_ROW_ID { get; set; }

    public Guid RUN_ID { get; set; }

    /// <summary>واحد صاحب اجرا.</summary>
    public string VAHEDCODE { get; set; } = null!;

    /// <summary>
    /// زیرواحد سطح اولِ زیر واحد اجرا که این سهم از اسنادش آمده (مثلاً اداره کل در اجرای ستاد)؛ برای
    /// اسناد خود واحد اجرا یا اجرای جداگانه = <see cref="VAHEDCODE"/>.
    /// </summary>
    public string SOURCE_VAHEDCODE { get; set; } = null!;

    public string ACCCODE { get; set; } = null!;

    public string? ACCNAME { get; set; }

    public decimal? AMOUNT_CUR { get; set; }

    public decimal? AMOUNT_PRV { get; set; }

    public virtual TB_FS_RUN_ROW RUN_ROW { get; set; } = null!;
}
