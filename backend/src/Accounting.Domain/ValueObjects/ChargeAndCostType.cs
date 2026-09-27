namespace Accounting.Domain.ValueObjects;

/// <summary>
/// Legacy kind flag for a <c>TB_CHARGEANDCOST_HEAD</c> row — نگاشت مقدار عددی ستون
/// <c>CHARGEANDCOST_TYPE</c> (که تا ۲۰۲۶-۰۹-۲۷ به‌اشتباه <c>bool</c> اسکفولد شده بود؛ رجوع به
/// ریسک باز #۲ در <c>CLAUDE.md</c> — این ستون دقیقاً همان الگوی <c>DOCLIFE</c>/<c>TYPECODE</c>
/// است که فازهای ۲۵/۲۷/۲۸ برایشان اصلاح کردند، فقط یک فاز دیرتر کشف شد).
///
/// Values confirmed by <c>docs/tankhah-khazaneh-module.md</c> §۱ against a live, read-only Oracle
/// query (۲۷ test rows) and by <c>docs/centralaccount-business-reference.md</c> §۲۴-۵-۴, which
/// quotes the reference project's own two-value flag for this column.
/// </summary>
public enum ChargeAndCostType
{
    /// <summary>شارژ تنخواه — ترمیم/افزایش سقف. Not created by the petty-cash module's chunk 1;
    /// reserved for the ترمیم/شارژ flow (design §۳ بخش سوم).</summary>
    Charge = 1,

    /// <summary>هزینه‌کرد — صورت‌هزینه تنخواه. This is the only value chunk 1's
    /// <c>CreatePettyCashExpenseDocCommand</c> ever writes.</summary>
    Cost = 2,
}
