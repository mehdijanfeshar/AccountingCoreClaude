using Accounting.Application.Common;
using Accounting.Application.PersonActions.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_PERSON_ACTION</c>. Deliberately separate from
/// <see cref="IPersonActionRepository"/> — never stages changes and always returns
/// <see cref="PersonActionDto"/> projections, never the Domain entity.
/// </summary>
public interface IPersonActionReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted rows ordered by <c>USERID</c> (which participates in
    /// <c>UK_PERSON_ACTION</c> but is not itself unique), then <c>ID</c> as a tie-breaker for
    /// stable paging.
    /// </summary>
    Task<PagedResult<PersonActionDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the row with the given <paramref name="id"/> regardless of its logical-delete
    /// state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<PersonActionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
