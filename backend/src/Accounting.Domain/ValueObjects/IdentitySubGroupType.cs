namespace Accounting.Domain.ValueObjects;

/// <summary>
/// نوع مقدار زیرگروه شناسنامه (تاریخ/حروف فارسی/عدد/حروف لاتین) — نگاشت مقدار عددی ستون
/// <c>TB_IDENTITYSUBGRP.SUBGRPS_TYPE</c> (فعلاً به‌اشتباه <c>bool?</c>؛ رجوع به ریسک باز #۲ در
/// <c>CLAUDE.md</c>) به این enum.
///
/// مقادیر از جدول قطعی (property-به-property با پروژهٔ مرجع <c>Tamin.Core</c>) در
/// <c>docs/centralaccount-business-reference.md</c> §۲۴-۱ آمده‌اند — قطعی‌اند، حدس زده نشده‌اند.
///
/// ⚠️ چهارمقداری است: مقدار قبلی <c>bool?</c> ساختاری فقط قادر به بیان دو مقدار (به‌علاوهٔ
/// <see langword="null"/>) بود؛ مقدار ۳ (<see cref="Number"/>) و ۴ (<see cref="LatinLetter"/>) از
/// طریق API قبلی اصلاً غیرقابل‌دسترس بودند.
/// </summary>
public enum IdentitySubGroupType
{
    /// <summary>تاریخ.</summary>
    Date = 1,

    /// <summary>حروف فارسی.</summary>
    PersianLetter = 2,

    /// <summary>عدد.</summary>
    Number = 3,

    /// <summary>حروف لاتین.</summary>
    LatinLetter = 4,
}
