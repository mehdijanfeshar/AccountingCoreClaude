namespace Accounting.Domain.ValueObjects;

/// <summary>
/// نوع مدرک بانکی رسید (فیش/حواله) — نگاشت مقدار عددی ستون <c>TB_RECEIP.RECEIPT_KIND</c> (فعلاً
/// به‌اشتباه non-nullable <c>bool</c>؛ رجوع به ریسک باز #۲ در <c>CLAUDE.md</c>) به این enum.
///
/// مقادیر از جدول قطعی (property-به-property با پروژهٔ مرجع <c>Tamin.Core</c>) در
/// <c>docs/centralaccount-business-reference.md</c> §۲۴-۱ آمده‌اند: <c>Fish = 1</c> (فیش)،
/// <c>Havale = 2</c> (حواله) — قطعی‌اند، حدس زده نشده‌اند.
/// </summary>
public enum ReceiptType
{
    /// <summary>فیش.</summary>
    Fish = 1,

    /// <summary>حواله.</summary>
    Havale = 2,
}
