namespace Accounting.Domain.ValueObjects;

/// <summary>
/// نوع اعلامیه از نظر بدهکار/بستانکار — نگاشت مقدار عددی ستون <c>TB_ELAMHEAD.ELAMH_CASE</c> (که تا
/// ۲۰۲۶-۰۹-۲۰ به‌اشتباه <c>bool?</c> اسکفولد شده بود؛ رجوع به ریسک باز #۲ در <c>CLAUDE.md</c>).
///
/// <b>این تنها ستون این خانواده است که هیچ ابهامی ندارد:</b> کامنت خودِ ستون Oracle
/// («نوع اعلاميه 1بد     2بس») و enum <c>Entities\Elam\ElamCase.cs</c> پروژهٔ مرجع
/// (<c>Debtor=1</c>, <c>Creditor=2</c>) <b>دقیقاً یکدیگر را تأیید می‌کنند</b> — برخلاف پنج موردی که
/// <c>docs/centralaccount-business-reference.md</c> §۱۰-۴ فهرست کرده.
///
/// ضمناً این ستون یکی از چهار شاهد مستقل «بدهکار = ۱» است که §۱۰-۴ برای تناقض حل‌نشدهٔ
/// <c>TB_ACCOUNTCODE.TYPEACTIVITY</c> برمی‌شمارد.
/// </summary>
public enum ElamCase
{
    /// <summary>بدهکار.</summary>
    Debtor = 1,

    /// <summary>بستانکار.</summary>
    Creditor = 2,
}
