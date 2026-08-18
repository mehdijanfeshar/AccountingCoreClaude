using Accounting.Application.Common;
using Accounting.Application.Vouchers.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_VOUCHERSHEAD</c>. Deliberately separate from
/// <see cref="IVoucherHeadRepository"/> (the write-side repository) — this repository never
/// stages changes and always returns <see cref="Vouchers.Queries.VoucherHeadDto"/> projections,
/// never the Domain entity.
/// </summary>
public interface IVoucherHeadReadRepository
{
    /// <summary>
    /// Returns a page of non-deleted voucher heads, optionally filtered by <paramref name="year"/>
    /// and/or <paramref name="vahedCode"/> when supplied, ordered by <c>YEAR</c>, <c>VAHEDCODE</c>,
    /// <c>DOC_NUM</c>, then <c>ID</c> as a tie-breaker for stable paging.
    /// </summary>
    Task<PagedResult<VoucherHeadDto>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? year,
        string? vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the voucher head with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists.
    /// </summary>
    Task<VoucherHeadDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
