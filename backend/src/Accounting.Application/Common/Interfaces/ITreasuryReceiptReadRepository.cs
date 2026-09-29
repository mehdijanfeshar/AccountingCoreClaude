using Accounting.Application.Treasury.Queries;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <see cref="Accounting.Domain.Entity.TB_TR_RECEIPT"/> — خزانه‌داری،
/// بخش ۴-ج (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Never stages changes, always returns DTO
/// projections.
/// </summary>
public interface ITreasuryReceiptReadRepository
{
    /// <summary>
    /// <c>GET receipts?state=&amp;search=&amp;pageNumber=&amp;pageSize=</c> — newest-created-first,
    /// plus per-state row counts (unaffected by <paramref name="state"/>/<paramref name="search"/>).
    /// <paramref name="search"/> matches <c>CODE</c>/<c>PAYER_NAME</c> (case-insensitive, substring).
    /// </summary>
    Task<ReceiptListResult> GetPagedAsync(
        int pageNumber,
        int pageSize,
        ReceiptState? state,
        string? search,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary><c>GET receipts/{id}</c>. Returns <see langword="null"/> when no row with that
    /// <c>ID</c> exists.</summary>
    Task<ReceiptDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET receipts/{id}/accounting</c> — projects <c>VOUCHER_ID</c> (if registered) into full
    /// voucher line detail via <see cref="IVoucherAccountingReader"/>, plus <c>PAYRECIVHEAD_ID</c>'s
    /// <c>PAYRECIVCODE</c>. Returns <see langword="null"/> when no row with that <c>ID</c> exists.
    /// </summary>
    Task<ReceiptAccountingDto?> GetAccountingAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);
}
