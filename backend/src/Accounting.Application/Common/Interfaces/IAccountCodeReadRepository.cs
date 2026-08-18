using Accounting.Application.Accounts.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_ACCOUNTCODE</c>. Deliberately separate from
/// <see cref="IAccountCodeRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="Accounts.Queries.AccountCodeDto"/> projections,
/// never the Domain entity.
/// </summary>
public interface IAccountCodeReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted account codes ordered by <c>ACCCODE</c>, then <c>ID</c>
    /// as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<AccountCodeDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the account code with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<AccountCodeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
