namespace Accounting.Domain.ValueObjects;

/// <summary>
/// ثابت/متغیر بودن زیرگروه شناسنامه — نگاشت مقدار عددی ستون <c>TB_IDENTITYSUBGRP.FIXED</c>
/// (فعلاً به‌اشتباه non-nullable <c>bool</c>؛ رجوع به ریسک باز #۲ در <c>CLAUDE.md</c>) به این
/// enum.
///
/// مقادیر از جدول قطعی (property-به-property با پروژهٔ مرجع <c>Tamin.Core</c>) در
/// <c>docs/centralaccount-business-reference.md</c> §۲۴-۱ آمده‌اند: <c>Fixed = 1</c> (ثابت)،
/// <c>Variable = 2</c> (متغیر) — قطعی‌اند، حدس زده نشده‌اند.
/// </summary>
public enum IdentitySubGroupKind
{
    /// <summary>ثابت.</summary>
    Fixed = 1,

    /// <summary>متغیر.</summary>
    Variable = 2,
}
