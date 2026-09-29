using Accounting.Application.Treasury.Queries;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <see cref="Accounting.Domain.Entity.TB_TR_TRANSFER"/> — خزانه‌داری،
/// بخش ۴-ج (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Never stages changes, always returns DTO
/// projections.
/// </summary>
public interface ITreasuryTransferReadRepository
{
    /// <summary>
    /// <c>GET transfers?state=&amp;search=&amp;pageNumber=&amp;pageSize=</c> — newest-created-first,
    /// plus per-state row counts. <paramref name="search"/> matches <c>CODE</c>/<c>REASON</c>
    /// (case-insensitive, substring).
    /// </summary>
    Task<TransferListResult> GetPagedAsync(
        int pageNumber,
        int pageSize,
        TransferState? state,
        string? search,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary><c>GET transfers/{id}</c> — with events. Returns <see langword="null"/> when no row
    /// with that <c>ID</c> exists.</summary>
    Task<TransferDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET transfers/{id}/accounting</c> — projects <c>VOUCHER_ID</c> (if approved) into full
    /// voucher line detail via <see cref="IVoucherAccountingReader"/>. Returns
    /// <see langword="null"/> when no row with that <c>ID</c> exists.
    /// </summary>
    Task<TransferAccountingDto?> GetAccountingAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every non-deleted transfer in <paramref name="vahedCode"/> currently
    /// <see cref="TransferState.PendingTreasurer"/> — the انتقال وجه half of
    /// <c>GET approval-cartable</c>'s third source (alongside درخواست پرداخت Pending* and تنخواه
    /// ترمیم <c>PendingTreasurer</c>), merged in C# — never a cross-module SQL join.
    /// </summary>
    Task<IReadOnlyList<TransferListItemDto>> GetPendingForCartableAsync(
        string vahedCode, CancellationToken cancellationToken = default);
}
