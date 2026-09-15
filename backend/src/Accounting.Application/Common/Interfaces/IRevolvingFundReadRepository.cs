using Accounting.Application.RevolvingFunds.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_REVOLVING_FUND</c>. Deliberately separate from
/// <see cref="IRevolvingFundRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="RevolvingFunds.Queries.RevolvingFundDto"/>
/// projections, never the Domain entity.
/// </summary>
public interface IRevolvingFundReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted revolving-fund rows belonging to <paramref name="vahedCode"/>,
    /// ordered by <c>CODE</c>, then <c>ID</c> as a tie-breaker for stable paging.
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
    Task<PagedResult<RevolvingFundDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the revolving-fund row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<RevolvingFundDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
