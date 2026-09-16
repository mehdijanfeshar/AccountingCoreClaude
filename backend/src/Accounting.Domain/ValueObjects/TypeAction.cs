namespace Accounting.Domain.ValueObjects;

/// <summary>
/// رفتار موردانتظار سیستم هنگام ثبت سندی که خلاف ماهیت حساب باشد (مثلاً حسابی با ماهیت
/// بدهکار که بستانکار شود) — نگاشت مقدار عددی ستون <c>TB_ACCOUNTCODE.TYPEACTION</c> (فعلاً
/// به‌اشتباه <c>bool?</c>، رجوع به ریسک باز #۲ در <c>CLAUDE.md</c>) به این enum. فقط روی سطح
/// معین ست می‌شود.
///
/// معادل enum <c>TypeAction</c> در پروژهٔ مرجع
/// (<c>D:\CentralAccount\Tamin.Core\Entities\AccountCodes\TypeAction.cs</c>).
///
/// ⚠️ **یافتهٔ کلیدی (رجوع به <c>docs/centralaccount-business-reference.md</c> §۱-۳):** در
/// پروژهٔ مرجع این مقدار **ذخیره و نمایش داده می‌شود ولی هرگز در هیچ اعتبارسنجی سندی مصرف
/// نمی‌شود** — نه <c>AddVoucherValidator</c>، نه <c>AddVoucherCommandHandler</c>، نه
/// <c>AddVoucherDetailValidator</c> به آن ارجاع نمی‌دهند. پس تعریف این enum به‌معنای پیاده‌سازی
/// رفتار «کنترل خلاف ماهیت» **نیست** — فقط یک برچسب طبقه‌بندی ذخیره‌شده است. هیچ منطقی نباید
/// بر این فرض ساخته شود که این رفتار در سیستم واقعاً اعمال می‌شود.
/// </summary>
public enum TypeAction
{
    /// <summary>کنترل نشود — خلاف ماهیت بی‌سروصدا پذیرفته می‌شود.</summary>
    NotControlled = 1,

    /// <summary>اخطار دهد — خلاف ماهیت با هشدار (غیرمسدودکننده) همراه می‌شود.</summary>
    Warning = 2,

    /// <summary>ثبت نشود — خلاف ماهیت باید رد شود.</summary>
    NotAdded = 3,
}
