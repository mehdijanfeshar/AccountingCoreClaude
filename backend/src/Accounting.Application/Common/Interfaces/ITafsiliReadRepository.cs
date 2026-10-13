using Accounting.Application.Common;
using Accounting.Application.Queries.Tafsilis;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_TAFSILI</c>. Deliberately separate from
/// <see cref="ITafsiliRepository"/> (the write-side repository) — this repository never stages
/// changes and always returns <see cref="TafsiliDto"/> projections, never the Domain entity.
/// </summary>
public interface ITafsiliReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted rows belonging to <paramref name="vahedCode"/>, ordered by
    /// <c>TAFSILI_CODE</c>, then <c>ID</c> as a tie-breaker for stable paging. A non-empty
    /// <paramref name="search"/> keeps only rows whose code or name contains it.
    /// </summary>
    Task<PagedResult<TafsiliDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        string? search,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the row with the given <paramref name="id"/> regardless of its logical-delete
    /// state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<TafsiliDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);
}
