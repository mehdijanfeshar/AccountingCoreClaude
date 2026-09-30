namespace Accounting.Domain.ValueObjects;

/// <summary>
/// ماهیت ردیف قالب — <c>TB_FS_TEMPLATE_ROW.NORMAL_BALANCE</c>. مقادیر داخلی همیشه با علامت
/// حسابداری (بدهکار مثبت) نگه داشته می‌شوند؛ ماهیت فقط در نمایش علامت را برمی‌گرداند (سند منبع §۷-۳).
/// </summary>
public enum FsNormalBalance
{
    /// <summary>بدهکار — مقدار همان‌طور نمایش داده می‌شود.</summary>
    Debit = 1,
    /// <summary>بستانکار — مقدار در نمایش قرینه می‌شود.</summary>
    Credit = 2,
}
