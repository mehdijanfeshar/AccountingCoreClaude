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
    /// Returns a page of non-deleted temporary-voucher header rows belonging to
    /// <paramref name="vahedCode"/>, ordered by <c>DATE_DOC</c> (nullable — Oracle sorts NULLs
    /// last), then <c>ID</c> as a tie-breaker for stable paging.
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
    Task<PagedResult<TmpVoucherHeadDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the temporary-voucher header row with the given <paramref name="id"/> regardless
    /// of its logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<TmpVoucherHeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
