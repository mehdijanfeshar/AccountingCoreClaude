using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_PC_SETTLEMENT_PERIOD"/> — settlement part 3-b
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9). Only stages changes — never calls
/// SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
///
/// <see cref="GetDraftAsync"/> and <see cref="GetLatestFinalAsync"/> are plain reads with no
/// side effects (same shape as <see cref="IPettyCashFundRepository.HasActiveExpenseDocsAsync"/> —
/// a query-ish helper living on a write-side repository is an established pattern in this
/// project). They are used both by command handlers (Count/Finalize, which mutate afterwards)
/// AND by <c>PettyCashSettlementPeriodProvisioner</c>'s read-only preview path — using them from
/// a query handler never triggers a write because nothing calls <c>SaveChangesAsync</c> there.
/// </summary>
public interface IPettyCashSettlementPeriodRepository
{
    Task AddAsync(TB_PC_SETTLEMENT_PERIOD period, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_PC_SETTLEMENT_PERIOD"/> by <c>ID</c> as change-tracked,
    /// verifying unit ownership. Returns <see langword="null"/> when no row with that <c>ID</c>
    /// exists.
    /// </summary>
    Task<TB_PC_SETTLEMENT_PERIOD?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// The single non-deleted, <see cref="Accounting.Domain.ValueObjects.PettyCashSettlementState.Draft"/>
    /// row for <paramref name="fundId"/>, change-tracked, or <see langword="null"/> when none
    /// exists yet (no period has ever been opened, or the last one was already finalized). There
    /// is at most one Draft row per fund at any time by construction — only
    /// <c>PettyCashSettlementPeriodProvisioner.EnsureDraftAsync</c> creates one, and only after
    /// checking this method first.
    /// </summary>
    Task<TB_PC_SETTLEMENT_PERIOD?> GetDraftAsync(Guid fundId, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// The most recently finalized (<see cref="Accounting.Domain.ValueObjects.PettyCashSettlementState.Final"/>)
    /// period for <paramref name="fundId"/> — ordered by <c>PERIOD_END</c> descending — or
    /// <see langword="null"/> when the fund has never had a period finalized. Its
    /// <c>PERIOD_END</c>/<c>COUNTED_BALANCE</c> anchor the next period's boundaries and opening
    /// balance (§۹ — "دورهٔ بعدی = بلافاصله بعد از آخرین دورهٔ Final").
    /// </summary>
    Task<TB_PC_SETTLEMENT_PERIOD?> GetLatestFinalAsync(Guid fundId, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when a non-deleted, <see cref="Accounting.Domain.ValueObjects.PettyCashSettlementState.Final"/>
    /// period of <paramref name="fundId"/> covers <paramref name="date"/> (شمسی
    /// <c>YYYYMMDD</c>, inclusive on both ends). Backs the "cannot delete a استرداد/ترمیم whose
    /// own date falls inside an already-finalized period" guard —
    /// <c>DeletePettyCashRefundCommandHandler</c>'s بخش ۳-ب TODO.
    /// </summary>
    Task<bool> ExistsFinalCoveringDateAsync(
        Guid fundId, string date, string vahedCode, CancellationToken cancellationToken = default);
}
