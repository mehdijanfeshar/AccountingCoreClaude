namespace Accounting.Application.PettyCash.Common;

/// <summary>
/// The single place §2/§3's موجودی نقد equation lives, so the funds list, the دشبورد, the ترمیم
/// پیش‌نمایش and <c>PettyCashSubmitRuleChecker</c> can never drift apart — same "one rule, one
/// home" shape as <c>Accounting.Application.Common.Security.PettyCashDocEditability</c>.
///
/// <b>The equation</b> (<c>docs/tankhah-khazaneh-module.md</c>، بخش ۳-الف — «فرمول موجودی نقد»):
/// <code>
/// موجودی نقد = CEILING − Σ(مبلغ اسناد New, PendingReview, Returned, Approved)
///             + Σ(TOTAL_AMOUNT ترمیم‌های Paid) + Σ(AMOUNT استردادهای حذف‌نشده)
/// </code>
///
/// <b>Why the full Approved amount is still subtracted even after a ترمیم پرداخت‌شده.</b> An
/// Approved صورت‌هزینه stays <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Approved"/>
/// — it never becomes <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Settled"/> until
/// بخش ۳-ب's دورهٔ تسویه runs. So <c>approvedAmount</c> here always includes every currently-Approved
/// document, whether or not it has already been paid back via a ترمیم. The "+Σ(TOTAL_AMOUNT
/// ترمیم‌های Paid)" term is what makes the equation balance anyway: once a ترمیم is Paid, its
/// linked documents' amount is still being subtracted (they are still Approved), but the exact
/// same amount (a ترمیم's <c>TOTAL_AMOUNT</c> is, by construction, the sum of the documents it
/// links — see <c>CreatePettyCashReplenishmentCommandHandler</c>) is added back by this term. Net
/// effect: paying a ترمیم returns موجودی نقد to (conceptually) the سقف level for that slice of
/// documents, without needing to touch <c>DOC_STATE</c> at all. This is the same reasoning
/// <c>docs/tankhah-khazaneh-module.md</c> spells out for بخش ۳-الف and explicitly confirms is
/// correct.
/// </summary>
public static class PettyCashBalanceCalculator
{
    /// <param name="ceiling">TB_PC_FUND.CEILING.</param>
    /// <param name="approvedAmount">Sum of AMOUNT_BEFORE_TAX+VAT_AMOUNT for every currently
    /// <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Approved"/> صورت‌هزینه of the fund
    /// — regardless of whether it is already linked to a ترمیم (Paid or not).</param>
    /// <param name="inFlightAmount">Same sum for New/PendingReview/Returned.</param>
    /// <param name="paidReplenishmentTotal">Sum of TOTAL_AMOUNT for every non-deleted
    /// <see cref="Accounting.Domain.ValueObjects.PettyCashReplenishmentState.Paid"/> ترمیم of the
    /// fund.</param>
    /// <param name="refundTotal">Sum of AMOUNT for every non-deleted استرداد of the fund.</param>
    public static decimal CashBalance(
        decimal ceiling,
        decimal approvedAmount,
        decimal inFlightAmount,
        decimal paidReplenishmentTotal,
        decimal refundTotal)
        => ceiling - approvedAmount - inFlightAmount + paidReplenishmentTotal + refundTotal;
}
