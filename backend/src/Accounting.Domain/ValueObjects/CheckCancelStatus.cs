namespace Accounting.Domain.ValueObjects;

/// <summary>
/// وضعیت ابطال برگ چک — نگاشت مقدار عددی ستون <c>TB_CHECK.EBTAL</c> (که تا ۲۰۲۶-۰۹-۲۰ به‌اشتباه
/// <c>bool</c> اسکفولد شده بود؛ رجوع به ریسک باز #۲ در <c>CLAUDE.md</c>) به این enum.
///
/// ⚠️ <b>اینجا یک تلهٔ واقعی وجود دارد.</b> در پروژهٔ مرجع <b>دو</b> enum هم‌نام <c>CheckStatus</c>
/// هست:
/// <list type="bullet">
///   <item><c>Entities\Checks\CheckStatus.cs</c> = <c>{Pay=1, UnPay=2, Cancle=3}</c> — سه‌مقداری</item>
///   <item><c>Entities\Checks\Enum\CheckStatus.cs</c> = <c>{canceled=1, notCanceled=2}</c> — دومقداری</item>
/// </list>
/// خودِ <c>Check.IsEbtal</c> صریحاً از نوع <b>دومی</b> است (<c>Check.cs:43</c>)، و §۲۴-۵-۱ سند مرجع
/// هم تأییدش می‌کند (<c>IsEbtal = notCanceled</c> هنگام ساخت برگ چک). جدول §۲۴-۱ همان سند enum
/// <b>اشتباهی</b> (سه‌مقداری) را نقل کرده — این خطا در فاز ۲۷ کشف و در
/// <c>docs/open-decisions.md</c> ثبت شد. اینجا عمداً نام <c>CheckCancelStatus</c> انتخاب شد تا
/// همان ابهامِ نام تکراری به مدل ما منتقل نشود.
///
/// ⚠️ راستی‌آزمایی روی دادهٔ زندهٔ <c>CENTRALACCOUNT</c> انجام نشده — رجوع به
/// <see cref="CheckPrintStatus"/> برای دلیلش (نبودِ مسیر نوشتن برای <c>TB_CHECK</c>).
/// </summary>
public enum CheckCancelStatus
{
    /// <summary>ابطال شده.</summary>
    Canceled = 1,

    /// <summary>ابطال نشده.</summary>
    NotCanceled = 2,
}
