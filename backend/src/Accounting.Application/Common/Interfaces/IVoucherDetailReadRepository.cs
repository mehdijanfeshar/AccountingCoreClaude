using Accounting.Application.Common;
using Accounting.Application.Vouchers.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_VOUCHERSDETAIL</c>. Deliberately separate from
/// <see cref="IVoucherDetailRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="Vouchers.Queries.VoucherDetailDto"/> projections,
/// never the Domain entity.
/// </summary>
public interface IVoucherDetailReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted voucher detail lines belonging to
    /// <paramref name="vahedCode"/>, optionally filtered further by
    /// <paramref name="voucherHeadId"/> and/or <paramref name="year"/> when supplied, ordered by
    /// <c>RADIF</c> then <c>ID</c> as a tie-breaker for stable paging. <c>RADIF</c> is
    /// <c>int?</c> in Legacy, so rows with a <see langword="null"/> <c>RADIF</c> sort last under
    /// Oracle's default ASC NULLS LAST behaviour.
    /// </summary>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="voucherHeadId">Optional exact-match filter on the VOUCHERSHEAD_ID column.</param>
    /// <param name="year">Optional exact-match filter on the YEAR column.</param>
    /// <param name="vahedCode">
    /// Organizational unit code to filter by — required, not nullable. Rows are matched with
    /// exact equality only (<c>VAHEDCODE == vahedCode</c>); rows with <c>VAHEDCODE IS NULL</c>
    /// are never returned to anyone, by deliberate fail-closed design (see implementation XML
    /// doc). Mirrors <c>IVoucherHeadReadRepository.GetPagedAsync</c>.
    /// </param>
    /// <param name="cancellationToken">Propagated to the underlying EF Core query.</param>
    Task<PagedResult<VoucherDetailDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? voucherHeadId,
        string? year,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the voucher detail line with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists. Deliberately no
    /// <c>ISDELETED</c> filter — mirrors <see cref="IVoucherHeadReadRepository.GetByIdAsync"/>.
    /// </summary>
    Task<VoucherDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
