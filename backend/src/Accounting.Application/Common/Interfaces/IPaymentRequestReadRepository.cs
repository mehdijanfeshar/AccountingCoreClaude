using Accounting.Application.Common;
using Accounting.Application.Treasury.Queries;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <see cref="Accounting.Domain.Entity.TB_TR_PAYMENT_REQUEST"/> —
/// خزانه‌داری، بخش ۴-الف (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Never stages changes, always
/// returns DTO projections.
/// </summary>
public interface IPaymentRequestReadRepository
{
    /// <summary>
    /// <c>GET payment-requests?state=&amp;search=&amp;pageNumber=&amp;pageSize=</c> — newest-
    /// created-first, plus the per-state row counts (unaffected by <paramref name="state"/>/
    /// <paramref name="search"/> so the کارتابل tab badges always show the full picture).
    /// <paramref name="search"/> matches <c>CODE</c>/<c>BENEFICIARY_NAME</c> (case-insensitive,
    /// substring).
    /// </summary>
    Task<PaymentRequestListResult> GetPagedAsync(
        int pageNumber,
        int pageSize,
        PaymentRequestState? state,
        string? search,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <c>GET payment-requests/{id}</c> — with events. Returns <see langword="null"/> when no row
    /// with that <c>ID</c> exists.
    /// </summary>
    Task<PaymentRequestDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every non-deleted request in <paramref name="vahedCode"/> currently in
    /// <see cref="PaymentRequestState.PendingUnitManager"/>/<see cref="PaymentRequestState.PendingFinanceManager"/>/
    /// <see cref="PaymentRequestState.PendingCeo"/> — the payment-request half of
    /// <c>GET approval-cartable</c>, merged in C# with the ترمیم half from
    /// <c>IPettyCashReplenishmentReadRepository</c>.
    /// </summary>
    Task<IReadOnlyList<PaymentRequestListItemDto>> GetPendingForCartableAsync(
        string vahedCode, CancellationToken cancellationToken = default);
}
