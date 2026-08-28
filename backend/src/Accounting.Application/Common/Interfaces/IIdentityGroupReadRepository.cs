using Accounting.Application.IdentityGroups.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_IDENTITYGROUP</c>. Deliberately separate from
/// <see cref="IIdentityGroupRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="IdentityGroups.Queries.IdentityGroupDto"/>
/// projections, never the Domain entity.
/// </summary>
public interface IIdentityGroupReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted identity-group rows ordered by <c>CREATEDDATE</c>, then
    /// <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<IdentityGroupDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the identity-group row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<IdentityGroupDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
