namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Legacy status flag for a <c>TB_CHARGEANDCOST_HEAD</c> row — نگاشت مقدار عددی ستون
/// <c>STATUS</c> (که تا ۲۰۲۶-۰۹-۲۷ به‌اشتباه <c>bool</c> اسکفولد شده بود؛ ریسک باز #۲).
///
/// ⚠️ <b>این enum از ۰ شروع می‌شود، نه از ۱ مثل بقیهٔ enumهای این پروژه.</b> این عمداً است، نه
/// فراموشی: مقدار زندهٔ دیده‌شده روی Oracle (۲۷ ردیف تستی، <c>docs/tankhah-khazaneh-module.md</c>
/// §۱) فقط ۰ و ۱ بود، و <c>docs/centralaccount-business-reference.md</c> §۲۴-۵-۴ enum مرجع را
/// دقیقاً <c>{temporary=0, reviewed=1, accepted=2}</c> نقل می‌کند. تغییر نقطهٔ شروع برای هماهنگی
/// ظاهری با enumهای دیگر پروژه، جعل دادهٔ Legacy می‌شد.
///
/// این enum مستقیماً توسط ماژول تنخواه نوشته نمی‌شود — تنها مصرف‌کننده‌اش
/// <c>PettyCashStatusMap</c> است، که <c>PettyCashDocState</c> هفت‌مقداری را به این سه مقدار
/// Legacy نگاشت می‌کند. هیچ Command ای مستقیماً این enum را از ورودی کاربر نمی‌گیرد.
/// </summary>
public enum ChargeAndCostStatus
{
    /// <summary>موقت.</summary>
    Temporary = 0,

    /// <summary>بررسی‌شده.</summary>
    Reviewed = 1,

    /// <summary>تایید دائم.</summary>
    Accepted = 2,
}
