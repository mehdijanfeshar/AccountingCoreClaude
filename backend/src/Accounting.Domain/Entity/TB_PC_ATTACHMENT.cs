using System;

namespace Accounting.Domain.Entity;

/// <summary>
/// «پیوست صورت‌هزینهٔ تنخواه» — جدول جانبی جدید (پیشوند <c>TB_PC_</c>، همان استثنای صریح صاحب
/// پروژه بر قانون «هیچ جدول جدیدی» که بخش ۱/۲ استفاده کرده‌اند). BLOB در دیتابیس، هم‌الگو با
/// <see cref="TB_ATTACH"/> Legacy — نه ستون جدید روی آن جدول (کلیدهایش <c>VOUCHERSHEAD_ID</c>/
/// <c>TAFSILI_ID</c>/<c>PAYRECEIVE_ID</c>اند، هیچ‌کدام اینجا صدق نمی‌کنند)، نه فایل‌سیستم/Blob
/// storage که در این پروژه سابقه ندارد (<c>docs/tankhah-khazaneh-module.md</c>، تصمیم‌های بخش ۲).
///
/// فقط وقتی سند والد (<see cref="TB_PC_EXPENSE_DOC.DOC_STATE"/>) در
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Draft"/> یا
/// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Returned"/> است قابل افزودن/حذف
/// است — همان قاعدهٔ <c>Accounting.Application.Common.Security.PettyCashDocEditability</c> که
/// خودِ سند استفاده می‌کند، کپی نشده. سقف اندازهٔ فایل (۱۰ مگابایت) در FluentValidation است، نه
/// در DB. <c>ID</c> بدون <c>DEFAULT sys_guid()</c> (ریسک #۱۱)؛ همیشه application-side تولید
/// می‌شود.
/// </summary>
public partial class TB_PC_ATTACHMENT
{
    public Guid ID { get; set; }

    public Guid EXPENSE_DOC_ID { get; set; }

    public string ATTACH_NAME { get; set; } = null!;

    public int ATTACH_SIZE { get; set; }

    public string? CONTENT_TYPE { get; set; }

    public byte[] ATTACH_FILE { get; set; } = null!;

    public int ATTACH_RADIF { get; set; }

    public DateTime CREATEDDATE { get; set; }

    public DateTime? UPDATEDDATE { get; set; }

    public string ADDUSERID { get; set; } = null!;

    public string? CHANGEUSERID { get; set; }

    public string? VAHEDCODE { get; set; }

    public string? YEAR { get; set; }

    public bool ISDELETED { get; set; }

    public virtual TB_PC_EXPENSE_DOC? EXPENSE_DOC { get; set; }
}
