using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Commands.Common;

/// <summary>
/// The single place "which settlement period is currently open for this fund, and what should its
/// OPENING_BALANCE be" is computed — بخش ۳-ب (<c>docs/tankhah-khazaneh-module.md</c> section 9).
/// Shared by <c>GetPettyCashFundSettlementPreviewQueryHandler</c> (read-only —
/// <see cref="ComputeCurrentAsync"/>) and <c>CountPettyCashSettlementCommandHandler</c>/
/// <c>FinalizePettyCashSettlementCommandHandler</c> (persist-if-missing —
/// <see cref="EnsureDraftAsync"/>), so the three can never disagree about period boundaries.
/// </summary>
public interface IPettyCashSettlementPeriodProvisioner
{
    /// <summary>
    /// Read-only — never stages an insert. Returns the existing
    /// <see cref="PettyCashSettlementState.Draft"/> row's snapshot if one exists, otherwise a
    /// freshly-computed, not-yet-persisted snapshot (<c>PeriodId = null</c>).
    /// </summary>
    Task<PettyCashSettlementPeriodSnapshot> ComputeCurrentAsync(
        TB_PC_FUND fund, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the existing <see cref="PettyCashSettlementState.Draft"/> row for the fund, or
    /// stages (via <c>IPettyCashSettlementPeriodRepository.AddAsync</c> — NOT saved) a new one
    /// computed the same way <see cref="ComputeCurrentAsync"/> would. The caller still owns the
    /// single <c>IUnitOfWork.SaveChangesAsync</c> call.
    /// </summary>
    Task<TB_PC_SETTLEMENT_PERIOD> EnsureDraftAsync(
        TB_PC_FUND fund, string vahedCode, CancellationToken cancellationToken = default);
}

/// <param name="PeriodId"><see langword="null"/> when no row has been persisted yet.</param>
public sealed record PettyCashSettlementPeriodSnapshot(
    Guid? PeriodId,
    string PeriodStart,
    string PeriodEnd,
    PettyCashSettlementState State,
    decimal OpeningBalance,
    decimal? CountedBalance);
