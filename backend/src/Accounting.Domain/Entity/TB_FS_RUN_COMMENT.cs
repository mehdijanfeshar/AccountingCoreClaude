using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// «نظر» روی یک اجرای صورت‌ها (ح-۳، سند منبع §۱۲-۳ و §۱۰): گفت‌وگوی بازبین و تهیه‌کننده روی یک ردیف
/// (<see cref="ROW_ID"/>)، یک کنترل (<see cref="CHECK_ID"/>) یا کل اجرا (هر دو خالی). فقط درج و حذف نرم. DDL 061.
/// </summary>
public partial class TB_FS_RUN_COMMENT
{
    public Guid ID { get; set; }

    public Guid RUN_ID { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    /// <summary><c>TB_FS_RUN_ROW.ID</c> — بدون FK (ردیف Snapshot).</summary>
    public Guid? ROW_ID { get; set; }

    /// <summary><c>TB_FS_RUN_CHECK.ID</c>.</summary>
    public Guid? CHECK_ID { get; set; }

    public string BODY { get; set; } = null!;

    public string ADDUSERID { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }

    public bool ISDELETED { get; set; }
}
