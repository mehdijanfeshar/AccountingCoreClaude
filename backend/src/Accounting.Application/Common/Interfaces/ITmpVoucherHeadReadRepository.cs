using Accounting.Application.Common;
using Accounting.Application.TmpVoucherHeads.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_TMP_VOUCHERHEAD</c>. Deliberately separate from
/// <see cref="ITmpVoucherHeadRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns
/// <see cref="TmpVoucherHeads.Queries.TmpVoucherHeadDto"/> projections, never the Domain entity.
/// </summary>
public interface ITmpVoucherHeadReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted temporary-voucher header rows ordered by <c>DATE_DOC</c>
    /// (nullable — Oracle sorts NULLs last), then <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<TmpVoucherHeadDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the temporary-voucher header row with the given <paramref name="id"/> regardless
    /// of its logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<TmpVoucherHeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
