namespace Accounting.Domain.ValueObjects;

/// <summary>
/// وضعیت اجرای تهیهٔ صورت‌های مالی — <c>TB_FS_RUN.STATE</c>. فاز ۴۵-ب فقط <see cref="Draft"/> را
/// می‌سازد؛ گردش تأیید (در بازبینی/تأییدشده/منتشرشده/جایگزین‌شده) در بخش ۴۵-د اضافه می‌شود.
/// </summary>
public enum FsRunState
{
    /// <summary>پیش‌نویس — تازه محاسبه‌شده.</summary>
    Draft = 1,
}
