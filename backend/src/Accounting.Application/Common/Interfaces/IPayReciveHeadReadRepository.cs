using Accounting.Application.Common;
using Accounting.Application.PayReciveHeads.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_PAYRECIVHEAD</c>. Deliberately separate from
/// <see cref="IPayReciveHeadRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns
/// <see cref="PayReciveHeads.Queries.PayReciveHeadDto"/> projections, never the Domain entity.
/// </summary>
public interface IPayReciveHeadReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted payment/receipt header rows ordered by
    /// <c>PAYRECIVCODE</c> (NOT NULL on this table, but not unique on its own — the table has no
    /// UNIQUE constraint at all), then <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<PayReciveHeadDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the payment/receipt header row with the given <paramref name="id"/> regardless of
    /// its logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<PayReciveHeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
