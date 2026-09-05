using Accounting.Application.ChequesIncorrents.Queries;
using Accounting.Application.Common;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_CHEQUES_INCORRENT</c>. Deliberately separate from
/// <see cref="IChequesIncorrentRepository"/> (the write-side repository) — this repository
/// never stages changes and always returns
/// <see cref="ChequesIncorrents.Queries.ChequesIncorrentDto"/> projections, never the Domain
/// entity.
/// </summary>
public interface IChequesIncorrentReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted <c>TB_CHEQUES_INCORRENT</c> rows ordered by
    /// <c>CHEQ_NO</c>, then <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<ChequesIncorrentDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the row with the given <paramref name="id"/> regardless of its logical-delete
    /// state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<ChequesIncorrentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
