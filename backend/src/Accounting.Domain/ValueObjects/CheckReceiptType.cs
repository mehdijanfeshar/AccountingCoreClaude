namespace Accounting.Domain.ValueObjects;

/// <summary>
/// نوع مدرک بانکی کارت بانک (چک صوری/چک واقعی/فیش/حواله) — نگاشت مقدار عددی ستون
/// <c>TB_BANKCARTDETAIL.CHECKRECEIPTTYPE</c> (فعلاً به‌اشتباه <c>bool?</c>؛ رجوع به ریسک باز #۲ در
/// <c>CLAUDE.md</c>) به این enum.
///
/// مقادیر از جدول قطعی (property-به-property با پروژهٔ مرجع <c>Tamin.Core</c>) در
/// <c>docs/centralaccount-business-reference.md</c> §۲۴-۱ آمده‌اند — قطعی‌اند، حدس زده نشده‌اند.
///
/// ⚠️ چهارمقداری است: مقدار قبلی <c>bool?</c> ساختاری فقط قادر به بیان دو مقدار (به‌علاوهٔ
/// <see langword="null"/>) بود؛ مقدار ۳ (<see cref="Fish"/>) و ۴ (<see cref="Havale"/>) از طریق
/// API قبلی اصلاً غیرقابل‌دسترس بودند.
/// </summary>
public enum CheckReceiptType
{
    /// <summary>چک صوری.</summary>
    SoriCheck = 1,

    /// <summary>چک واقعی.</summary>
    RealCheck = 2,

    /// <summary>فیش.</summary>
    Fish = 3,

    /// <summary>حواله.</summary>
    Havale = 4,
}
