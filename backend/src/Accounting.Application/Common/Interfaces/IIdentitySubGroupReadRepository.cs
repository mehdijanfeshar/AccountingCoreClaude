using Accounting.Application.IdentitySubGroups.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_IDENTITYSUBGRP</c>. Deliberately separate from
/// <see cref="IIdentitySubGroupRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="IdentitySubGroups.Queries.IdentitySubGroupDto"/>
/// projections, never the Domain entity.
/// </summary>
public interface IIdentitySubGroupReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted identity-sub-group rows belonging to
    /// <paramref name="vahedCode"/>, ordered by <c>CREATEDDATE</c>, then <c>ID</c> as a
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
    Task<PagedResult<IdentitySubGroupDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the identity-sub-group row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<IdentitySubGroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
