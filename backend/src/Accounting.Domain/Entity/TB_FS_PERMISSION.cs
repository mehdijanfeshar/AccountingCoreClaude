using System;
using Accounting.Domain.ValueObjects;

namespace Accounting.Domain.Entity;

/// <summary>
/// دسترسی یک کاربر در صورت‌های مالی (ط-۲): روی واحد <see cref="VAHEDCODE"/> (و اگر <see cref="INCLUDE_SUB"/>
/// زیرمجموعه‌اش) عملیات <see cref="OPERATIONS"/>. تا وقتی هیچ ردیفی تعریف نشده، همه مثل قبل مجازند. DDL 062.
/// </summary>
public partial class TB_FS_PERMISSION
{
    public Guid ID { get; set; }

    public string USERID { get; set; } = null!;

    public string? USERNAME { get; set; }

    public string VAHEDCODE { get; set; } = null!;

    public bool INCLUDE_SUB { get; set; }

    public FsOperation OPERATIONS { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public bool ISDELETED { get; set; }
}
