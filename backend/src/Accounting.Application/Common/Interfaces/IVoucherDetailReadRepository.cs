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
    /// Returns a page of non-deleted voucher detail lines, optionally filtered by
    /// <paramref name="voucherHeadId"/>, <paramref name="year"/> and/or
    /// <paramref name="vahedCode"/> when supplied, ordered by <c>RADIF</c> then <c>ID</c> as a
    /// tie-breaker for stable paging. <c>RADIF</c> is <c>int?</c> in Legacy, so rows with a
    /// <see langword="null"/> <c>RADIF</c> sort last under Oracle's default ASC NULLS LAST
    /// behaviour.
    /// </summary>
    Task<PagedResult<VoucherDetailDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Guid? voucherHeadId,
        string? year,
        string? vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the voucher detail line with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists. Deliberately no
    /// <c>ISDELETED</c> filter — mirrors <see cref="IVoucherHeadReadRepository.GetByIdAsync"/>.
    /// </summary>
    Task<VoucherDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
