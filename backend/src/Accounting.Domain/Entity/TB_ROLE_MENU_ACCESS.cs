using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// دسترسی یک نقش به یک منو (DDL 075، فاز ۵۴). <see cref="ACCESS_LEVEL"/>: ۰ بدون دسترسی، ۱ مشاهده، ۲ ثبت و تغییر.
/// ردیف حذف نمی‌شود؛ «بدون دسترسی» با سطح ۰ ذخیره می‌شود.
/// </summary>
public partial class TB_ROLE_MENU_ACCESS
{
    public Guid ID { get; set; }

    public string ROLE_NAME { get; set; } = null!;

    public string MENU_KEY { get; set; } = null!;

    public int ACCESS_LEVEL { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public DateTime CREATEDDATE { get; set; }

    public string? CHANGEUSERID { get; set; }

    public DateTime? UPDATEDDATE { get; set; }
}
