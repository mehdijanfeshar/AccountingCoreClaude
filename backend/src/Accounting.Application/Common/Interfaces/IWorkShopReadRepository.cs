using Accounting.Application.WorkShops.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_WORKSHOP</c>. Deliberately separate from
/// <see cref="IWorkShopRepository"/> (the write-side repository) — this repository never stages
/// changes and always returns <see cref="WorkShops.Queries.WorkShopDto"/> projections, never the
/// Domain entity.
/// </summary>
public interface IWorkShopReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted workshop rows belonging to <paramref name="vahedCode"/>,
    /// ordered by <c>WORKSHOPCODE</c>, then <c>ID</c> as a tie-breaker for stable paging.
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
    Task<PagedResult<WorkShopDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the workshop row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    ///
    /// <para>
    /// <paramref name="vahedCode"/> is the caller's own unit, server-assigned by
    /// <c>VahedScopeBehavior</c> — never client input. If the row exists but belongs to another
    /// unit this throws
    /// <see cref="Accounting.Application.Common.Exceptions.UnitAccessDeniedException"/> (403)
    /// rather than returning it. The parameter is required rather than optional on purpose: a
    /// lookup that does not state whose row it may return is exactly the hole that IDOR risk #1
    /// describes, so it must not be expressible.
    /// </para>
    /// </summary>
    Task<WorkShopDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);
}
