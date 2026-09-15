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
    /// Returns a page of non-deleted payment/receipt header rows belonging to
    /// <paramref name="vahedCode"/>, ordered by <c>PAYRECIVCODE</c> (NOT NULL on this table, but
    /// not unique on its own — the table has no UNIQUE constraint at all), then <c>ID</c> as a
    /// tie-breaker for stable paging.
    /// </summary>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="vahedCode">
    /// Organizational unit code to filter by — required, not nullable. Rows are matched with
    /// exact equality only (<c>VAHEDCODE == vahedCode</c>); rows with <c>VAHEDCODE IS NULL</c>
    /// are never returned to anyone, by deliberate fail-closed design (see implementation XML
    /// doc).
    /// </param>
    /// <param name="cancellationToken">Propagated to the underlying EF Core query.</param>
    Task<PagedResult<PayReciveHeadDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the payment/receipt header row with the given <paramref name="id"/> regardless of
    /// its logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<PayReciveHeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
