namespace Accounting.Domain.ValueObjects;

/// <summary>
/// وضعیت نسخهٔ قالب — <c>TB_FS_TEMPLATE_VERSION.STATE</c>. فقط <see cref="Draft"/> قابل ویرایش است؛
/// <see cref="Active"/> تغییرناپذیر (<c>docs/fs-module.md</c> §۲). گذارها سمت Application کنترل
/// می‌شوند (قاعدهٔ ۲ CLAUDE.md).
/// </summary>
public enum FsTemplateVersionState
{
    /// <summary>پیش‌نویس — قابل ویرایش.</summary>
    Draft = 1,
    /// <summary>فعال — تغییرناپذیر؛ از <c>EFFECTIVE_FROM_YEAR</c> به بعد استفاده می‌شود.</summary>
    Active = 2,
    /// <summary>بازنشسته — نسخهٔ فعالی که نسخهٔ دیگری با همان سال شروع جایش را گرفته.</summary>
    Retired = 3,
}
