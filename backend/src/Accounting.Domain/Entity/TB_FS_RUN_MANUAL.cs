using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// مقدار دستی یک ردیف «مقدار دستی» در یک اجرا، با دلیل (فاز ۴۵-ه). مبلغ به علامت نمایشی (همان که کاربر
/// وارد کرد)؛ موتور با ماهیت ردیف به علامت حسابداری تبدیل می‌کند.
/// </summary>
public partial class TB_FS_RUN_MANUAL
{
    public Guid ID { get; set; }

    public Guid RUN_ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public string TEMPLATE_CODE { get; set; } = null!;

    public string ROW_CODE { get; set; } = null!;

    public decimal? AMOUNT_CUR { get; set; }

    public decimal? AMOUNT_PRV { get; set; }

    public string REASON { get; set; } = null!;

    public string ADDUSERID { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }

    public virtual TB_FS_RUN RUN { get; set; } = null!;
}
