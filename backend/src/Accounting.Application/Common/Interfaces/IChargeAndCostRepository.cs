using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for the Legacy <c>TB_CHARGEANDCOST_HEAD</c>/<c>TB_CHARGEANDCOST_DETAIL</c>
/// pair, as consumed by the petty-cash module. This is NOT an independent CRUD surface — per
/// <c>docs/centralaccount-business-reference.md</c> §24-5-4 ("Create یک سرصفحه + دقیقاً یک
/// دیتیل می‌سازد") and <c>docs/tamin-core-entity-reference.md</c>, the reference project itself
/// treats this pair as a composite aggregate, never a Head/Detail-list CRUD. Only stages changes
/// — never calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="IUnitOfWork"/>.
/// </summary>
public interface IChargeAndCostRepository
{
    Task AddHeadAsync(TB_CHARGEANDCOST_HEAD head, CancellationToken cancellationToken = default);

    Task AddDetailAsync(TB_CHARGEANDCOST_DETAIL detail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_CHARGEANDCOST_HEAD"/> by <c>ID</c> as change-tracked,
    /// verifying unit ownership the same way every other by-id repository method in this project
    /// does (see <c>VahedOwnership</c>). Returns <see langword="null"/> when no row with that
    /// <c>ID</c> exists.
    /// </summary>
    Task<TB_CHARGEANDCOST_HEAD?> GetHeadForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the single change-tracked <see cref="TB_CHARGEANDCOST_DETAIL"/> row belonging to
    /// <paramref name="headId"/>, or <see langword="null"/> if none exists — by design there is
    /// never more than one (see this interface's XML doc).
    /// </summary>
    Task<TB_CHARGEANDCOST_DETAIL?> GetSingleDetailForUpdateAsync(Guid headId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the next <c>CHARGEANDCOST_CODE</c> (a zero-padded, 5-digit numeric string) for the
    /// given unit/year/type: the highest existing numeric code among ALL rows (including
    /// soft-deleted ones, so a deleted document's code is never handed out twice) plus one, per
    /// <c>docs/tankhah-khazaneh-module.md</c> §4. Mirrors
    /// <c>IdentityHeadRepository.GetNextSerialAsync</c>'s "highest + 1, gaps allowed" shape.
    /// </summary>
    Task<int> GetNextCodeAsync(string vahedCode, string year, ChargeAndCostType type, CancellationToken cancellationToken = default);
}
