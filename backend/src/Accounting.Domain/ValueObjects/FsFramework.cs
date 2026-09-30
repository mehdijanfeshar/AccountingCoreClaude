namespace Accounting.Domain.ValueObjects;

/// <summary>
/// مجموعهٔ صورت‌های مالی (چارچوب گزارشگری) — <c>TB_FS_TEMPLATE.FRAMEWORK</c>. فاز ۴۵
/// (<c>docs/fs-module.md</c> §۲).
/// </summary>
public enum FsFramework
{
    /// <summary>طرح بیمه‌ای — استاندارد ۲۷ (طرح‌های مزایای بازنشستگی).</summary>
    Pension = 1,
    /// <summary>واحد تجاری — استاندارد ۱.</summary>
    Commercial = 2,
    /// <summary>بخش عمومی.</summary>
    Public = 3,
    /// <summary>مدیریتی — قالب آزاد.</summary>
    Management = 4,
}
