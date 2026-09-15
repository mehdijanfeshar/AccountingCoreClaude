using Accounting.Application.AccountExceptions.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_ACCOUNTEXCEPTION</c>. Deliberately separate from
/// <see cref="IAccountExceptionRepository"/> — never stages changes and always returns
/// <see cref="AccountExceptionDto"/> projections, never the Domain entity.
/// </summary>
public interface IAccountExceptionReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted rows ordered by <c>CREATEDDATE</c>, then <c>ID</c> as a
    /// tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<AccountExceptionDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the row with the given <paramref name="id"/> regardless of its logical-delete
    /// state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<AccountExceptionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
