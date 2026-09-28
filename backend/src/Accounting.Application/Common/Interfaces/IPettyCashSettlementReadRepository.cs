using Accounting.Application.PettyCash.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository backing the settlement preview/finalize numbers — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9). Never stages changes; always returns DTO
/// projections, never the Domain entity. Shared by <c>GetPettyCashFundSettlementPreviewQueryHandler</c>
/// AND <c>FinalizePettyCashSettlementCommandHandler</c> so the two can never disagree about which
/// documents/amounts a settlement covers — same "preview and act read the same numbers" shape as
/// <c>GetPettyCashReplenishmentPreviewQueryHandler</c>/<c>CreatePettyCashReplenishmentCommandHandler</c>.
/// </summary>
public interface IPettyCashSettlementReadRepository
{
    /// <summary>
    /// Everything a settlement period's own math needs for one (fund, period) pair:
    /// <list type="bullet">
    /// <item><description>Every currently <see cref="Accounting.Domain.ValueObjects.PettyCashDocState.Approved"/>
    /// (not yet Settled), non-deleted صورت‌هزینه of the fund whose <c>CHARGEANDCOST_DATE</c>
    /// (تاریخ ثبت) is <![CDATA[<=]]> <paramref name="periodEnd"/> — <b>no lower bound</b>, per §۹'s
    /// literal rule ("تاریخ ثبت ≤ PERIOD_END"), so a صورت‌هزینه approved late (after an earlier
    /// period had already been finalized without it) is still picked up here rather than lost.
    /// Grouped by <c>TB_CHARGEANDCOST_DETAIL.EXPENSE_ID</c> (not by حساب معین directly) because
    /// <c>TB_EXPENCE_LINK_TAFSILI</c> is itself keyed by <c>EXPENSE_ID</c> — grouping by مادهٔ
    /// هزینه (<c>EXPENSE_ID</c>) is exactly grouping by (حساب, مجموعهٔ تفصیلی) at once, per §۹'s
    /// "ردیف جدا به‌ازای هر ترکیب (حساب، مجموعهٔ تفصیلی)" rule.</description></item>
    /// <item><description>The Paid ترمیم + non-deleted استرداد total for the period's own date
    /// range (<paramref name="periodStart"/>..<paramref name="periodEnd"/> inclusive) — kept
    /// deliberately separate from <c>IPettyCashLedgerReadRepository</c> (a different, broader
    /// report) rather than reused: the ledger's opening-balance/closing-balance math folds in
    /// <i>every</i> currently-Approved-or-Settled document dated before its <c>from</c>, which is
    /// not the same "locked-in previous period" semantics §۹ specifies (see
    /// <c>PettyCashSettlementPeriodProvisioner</c> XML doc for the opening-balance rule this
    /// method deliberately does NOT compute — that is anchored to the previous
    /// <see cref="Accounting.Domain.ValueObjects.PettyCashSettlementState.Final"/> period's own
    /// stored <c>COUNTED_BALANCE</c>, not re-derived live).</description></item>
    /// </list>
    /// </summary>
    Task<PettyCashSettlementMovementDto> GetPeriodMovementAsync(
        Guid fundId,
        string vahedCode,
        string periodStart,
        string periodEnd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The fund's own حساب معین تنخواه تفصیلی assignments (<c>TB_PC_FUND_LINK_TAFSILI</c>), as
    /// display/voucher-ready DTOs — the credit line's تفصیلی set. Empty when the fund has none
    /// configured (a valid state; the voucher credit line would then carry no تفصیلی, and
    /// <see cref="IVoucherTafsiliLevelGuard"/> decides whether that is acceptable for the fund's
    /// معین).
    /// </summary>
    Task<IReadOnlyList<PettyCashSettlementTafsiliDto>> GetFundTafsilisAsync(
        Guid fundId, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET funds/{fundId}/settlements</c> — every finalized period, newest-first.
    /// </summary>
    Task<IReadOnlyList<PettyCashSettlementHistoryItemDto>> GetFinalHistoryAsync(
        Guid fundId, string vahedCode, CancellationToken cancellationToken = default);
}
