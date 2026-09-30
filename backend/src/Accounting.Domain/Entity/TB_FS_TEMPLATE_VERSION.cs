using System;
using System.Collections.Generic;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// نسخهٔ یک قالب صورت مالی. فقط <see cref="FsTemplateVersionState.Draft"/> قابل ویرایش است؛ نسخهٔ
/// <see cref="FsTemplateVersionState.Active"/> تغییرناپذیر است تا اجرای سال‌های گذشته همیشه با قالب
/// همان زمان بازتولید شود (سند منبع §۱۱). فاز ۴۵-الف (<c>docs/fs-module.md</c> §۲).
/// </summary>
public partial class TB_FS_TEMPLATE_VERSION
{
    public Guid ID { get; set; }

    public Guid TEMPLATE_ID { get; set; }

    /// <summary>شمارهٔ نسخه در قالب، از ۱، هرگز دوباره استفاده نمی‌شود.</summary>
    public int VERSION_NO { get; set; }

    public FsTemplateVersionState STATE { get; set; }

    /// <summary>
    /// سال مالی‌ای که این نسخه از آن به بعد استفاده می‌شود — فقط برای نسخهٔ فعال/بازنشسته. موتور
    /// برای سال Y نسخهٔ فعالی را برمی‌دارد که بزرگ‌ترین <c>EFFECTIVE_FROM_YEAR &lt;= Y</c> را دارد.
    /// </summary>
    public int? EFFECTIVE_FROM_YEAR { get; set; }

    public string? DESCRIPTION { get; set; }

    public string? ACTIVATED_BY { get; set; }

    public DateTime? ACTIVATED_DATE { get; set; }

    /// <summary>SHA-256 محتوای ردیف‌ها در لحظهٔ فعال‌سازی (hex).</summary>
    public string? CONTENT_HASH { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_FS_TEMPLATE TEMPLATE { get; set; } = null!;

    public virtual ICollection<TB_FS_TEMPLATE_ROW> TB_FS_TEMPLATE_ROWs { get; set; } = new List<TB_FS_TEMPLATE_ROW>();
}
