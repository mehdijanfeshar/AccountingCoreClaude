using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// کپی یک ردیف قالب در لحظهٔ اجرا + مبلغ هر ستون (فقط درج). مبلغ با علامت حسابداری است (بدهکار
/// مثبت)؛ <see cref="NORMAL_BALANCE"/> فقط در نمایش علامت را برمی‌گرداند. فاز ۴۵-ب.
/// </summary>
public partial class TB_FS_RUN_ROW
{
    public Guid ID { get; set; }

    public Guid RUN_STATEMENT_ID { get; set; }

    /// <summary>تکرار شناسهٔ اجرا برای فیلتر بدون جوین.</summary>
    public Guid RUN_ID { get; set; }

    /// <summary>واحد صاحب اجرا — تکرار <c>TB_FS_RUN.VAHEDCODE</c>.</summary>
    public string VAHEDCODE { get; set; } = null!;

    public string ROW_CODE { get; set; } = null!;

    public string? PARENT_CODE { get; set; }

    public int ORDER_NO { get; set; }

    public FsRowType ROW_TYPE { get; set; }

    public string? TITLE_FA { get; set; }

    public string? TITLE_EN { get; set; }

    public string? NOTE_REF { get; set; }

    public FsNormalBalance? NORMAL_BALANCE { get; set; }

    public string? SELECTOR { get; set; }

    public FsValueType? VALUE_TYPE { get; set; }

    public string? FORMULA { get; set; }

    public string? FORMAT_JSON { get; set; }

    public bool IS_DRILLABLE { get; set; }

    /// <summary>ستون دورهٔ جاری؛ <see langword="null"/> برای ردیف بدون مقدار.</summary>
    public decimal? AMOUNT_CUR { get; set; }

    /// <summary>ستون سال قبل؛ <see langword="null"/> اگر اجرا ستون سال قبل ندارد.</summary>
    public decimal? AMOUNT_PRV { get; set; }

    public virtual TB_FS_RUN_STATEMENT RUN_STATEMENT { get; set; } = null!;

    public virtual ICollection<TB_FS_RUN_ACCOUNT> TB_FS_RUN_ACCOUNTs { get; set; } = new List<TB_FS_RUN_ACCOUNT>();
}
