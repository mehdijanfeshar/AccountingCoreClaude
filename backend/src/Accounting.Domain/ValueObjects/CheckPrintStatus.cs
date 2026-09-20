namespace Accounting.Domain.ValueObjects;

/// <summary>
/// وضعیت چاپ برگ چک — نگاشت مقدار عددی ستون <c>TB_CHECK.PRINT</c> (که تا ۲۰۲۶-۰۹-۲۰ به‌اشتباه
/// <c>bool</c> اسکفولد شده بود؛ رجوع به ریسک باز #۲ در <c>CLAUDE.md</c>) به این enum.
///
/// مقادیر مستقیماً از <c>Entities\Checks\Enum\CheckPrintStatus.cs</c> پروژهٔ مرجع خوانده شده‌اند
/// (<c>None=1</c> «چاپ نشده»، <c>Printed=2</c> «چاپ شده») — حدس زده نشده‌اند. جدول مرجع:
/// <c>docs/centralaccount-business-reference.md</c> §۲۴-۱ ردیف <c>TB_CHECK.PRINT</c>.
///
/// ⚠️ برخلاف ۱۸ ستون فازهای قبلی، این ستون روی دادهٔ زندهٔ <c>CENTRALACCOUNT</c> راستی‌آزمایی
/// **نشده** — هیچ مسیر نوشتنی برای <c>TB_CHECK</c> در API ما وجود ندارد (جدول تعبیه‌شده است)،
/// پس تنها پیامد ممکن، خواندنِ ردیفی با مقدار خارج از این دو است.
/// </summary>
public enum CheckPrintStatus
{
    /// <summary>چاپ نشده.</summary>
    None = 1,

    /// <summary>چاپ شده.</summary>
    Printed = 2,
}
