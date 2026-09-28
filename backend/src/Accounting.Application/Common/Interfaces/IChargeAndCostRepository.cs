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

    /// <summary>
    /// Stages a new <see cref="TB_CHARGE_LINK_COST"/> row — «کدام هزینه با کدام شارژ ترمیم شد»
    /// (بخش ۳-الف، <c>docs/tankhah-khazaneh-module.md</c>). Only stages — never calls
    /// SaveChanges.
    /// </summary>
    Task AddLinkAsync(TB_CHARGE_LINK_COST link, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when at least one non-deleted <see cref="TB_CHARGE_LINK_COST"/> row already links
    /// <paramref name="costId"/> (a <c>TB_CHARGEANDCOST_DETAIL.ID</c>) to any ترمیم — backs the
    /// «هیچ سندی دو بار ترمیم نمی‌شود» rule, checked immediately before each link is inserted so a
    /// race between two concurrent ترمیم requests over the same صورت‌هزینه is still caught (a
    /// unique DB constraint is not planned for this pair, so this is the only guard).
    /// </summary>
    Task<bool> ExistsActiveLinkForCostAsync(Guid costId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every non-deleted <see cref="TB_CHARGE_LINK_COST"/> row for <paramref name="chargeId"/> (a
    /// ترمیم's <c>TB_CHARGEANDCOST_HEAD.ID</c>), as change-tracked entities — used by
    /// <c>RejectPettyCashReplenishmentCommandHandler</c>/<c>DeletePettyCashReplenishmentCommandHandler</c>
    /// to soft-delete every link so the underlying صورت‌هزینه rows become replenishable again.
    /// </summary>
    Task<IReadOnlyList<TB_CHARGE_LINK_COST>> GetActiveLinksByChargeIdAsync(
        Guid chargeId, CancellationToken cancellationToken = default);
}
