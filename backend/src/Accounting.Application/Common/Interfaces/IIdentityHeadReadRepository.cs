using Accounting.Application.Common;
using Accounting.Application.IdentityHeads.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_IDENTITYHEAD</c>. Deliberately separate from
/// <see cref="IIdentityHeadRepository"/> (the write side) — this repository never stages changes
/// and always returns <see cref="IdentityHeadDto"/> projections, never the Domain entity.
/// </summary>
public interface IIdentityHeadReadRepository
{
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="vahedCode">
    /// Organizational unit code to filter by — required, not nullable. Matched with exact
    /// equality only; this is a security scope, not a narrowing filter, so it is applied
    /// unconditionally.
    /// </param>
    /// <param name="identityGroupId">
    /// Optional: restrict to one identity group. Null means "do not narrow".
    /// </param>
    /// <param name="year">Optional: restrict to one fiscal year.</param>
    /// <param name="cancellationToken">Propagated to the underlying EF Core query.</param>
    Task<PagedResult<IdentityHeadDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        Guid? identityGroupId = null,
        string? year = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the row with the given <paramref name="id"/> regardless of its logical-delete
    /// state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<IdentityHeadDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);
}
