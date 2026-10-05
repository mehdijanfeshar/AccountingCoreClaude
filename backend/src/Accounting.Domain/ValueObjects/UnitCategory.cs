namespace Accounting.Domain.ValueObjects;

/// <summary>
/// گروه واحدها (تصمیم صاحب پروژه ۲۰۲۶-۱۰-۰۵) — از روی نوع واحد؛ نگاشت در
/// <c>Accounting.Application.Common.Security.UnitCategories</c>. <c>TB_FS_RUN.UNIT_CATEGORY</c> (DDL 065).
/// </summary>
public enum UnitCategory
{
    /// <summary>بیمه‌ای — اداره‌کل، شعبه.</summary>
    Insurance = 1,

    /// <summary>درمانی — بیمارستان، درمانگاه، پلی‌کلینیک، … مدیریت درمان.</summary>
    Medical = 2,

    /// <summary>ستادی — ستاد مرکزی و ادارات ستادی.</summary>
    Headquarters = 3,
}
