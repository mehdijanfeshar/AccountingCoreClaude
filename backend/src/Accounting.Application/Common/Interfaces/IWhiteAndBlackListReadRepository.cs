using Accounting.Application.WhiteAndBlackLists.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_WHITEANDBLACKLIST</c>. Deliberately separate from
/// <see cref="IWhiteAndBlackListRepository"/> (the write-side repository) — this repository
/// never stages changes and always returns
/// <see cref="WhiteAndBlackLists.Queries.WhiteAndBlackListDto"/> projections, never the Domain
/// entity.
/// </summary>
public interface IWhiteAndBlackListReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted white/black-list rows ordered by <c>CREATEDDATE</c>, then
    /// <c>ID</c> as a tie-breaker for stable paging, narrowed by <paramref name="filter"/>.
    /// Pass <see cref="WhiteAndBlackListFilter.None"/> for an unfiltered page.
    /// </summary>
    Task<PagedResult<WhiteAndBlackListDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        WhiteAndBlackListFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the white/black-list row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<WhiteAndBlackListDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the <see cref="WhiteAndBlackListKey"/> of every non-deleted row whose
    /// <c>ACCOUNTCODE_ID</c> is in <paramref name="accountCodeIds"/>.
    ///
    /// Exists so the bulk-create use case can drop the combinations that already exist
    /// <b>before</b> staging anything, rather than letting Oracle reject the whole transaction on
    /// <c>UK_WHITEANDBLACKLIST</c>. It is a best-effort pre-check, not a lock: a concurrent insert
    /// between this read and the save still surfaces as the central duplicate-key → 409 mapping,
    /// which is the correct outcome.
    /// </summary>
    Task<IReadOnlyCollection<WhiteAndBlackListKey>> GetExistingKeysAsync(
        IReadOnlyCollection<Guid> accountCodeIds,
        CancellationToken cancellationToken = default);
}
