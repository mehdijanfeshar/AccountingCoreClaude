using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_TR_PAYMENT_REQUEST"/> ("درخواست پرداخت") — خزانه‌داری،
/// بخش ۴-الف (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Only stages changes — never calls
/// SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IPaymentRequestRepository
{
    Task AddAsync(TB_TR_PAYMENT_REQUEST paymentRequest, CancellationToken cancellationToken = default);

    /// <summary>Loads a single request by <c>ID</c>, change-tracked, scoped to
    /// <paramref name="vahedCode"/> via <c>VahedOwnership</c>. Returns <see langword="null"/> when
    /// no row with that <c>ID</c> exists.</summary>
    Task<TB_TR_PAYMENT_REQUEST?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Next value of the per-(VAHEDCODE, YEAR) 6-digit counter behind <c>CODE</c> ("PAY-" +
    /// this value, zero-padded) — same client-side-parsed-MAX approach as
    /// <c>ChargeAndCostRepository.GetNextCodeAsync</c> (a varchar column, lexicographic MAX would
    /// not sort numerically). Deleted rows are counted on purpose, for the same reason that method
    /// documents: a soft-deleted request's code must never be handed out again.
    /// </summary>
    Task<int> GetNextCodeAsync(string vahedCode, string year, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when at least one other non-deleted, non-<see cref="PaymentRequestState.Rejected"/>
    /// request in <paramref name="vahedCode"/> already carries the same
    /// (<paramref name="beneficiaryNationalId"/>, <paramref name="invoiceRef"/>) pair — the Submit-
    /// only duplicate rule (both must be non-empty on the caller's side before this is invoked).
    /// <paramref name="excludeId"/> is the request's own id being (re-)submitted, so it never
    /// matches itself. Uses <c>CountAsync(...) &gt; 0</c>, never <c>AnyAsync</c>.
    /// </summary>
    Task<bool> ExistsDuplicateAsync(
        string beneficiaryNationalId,
        string invoiceRef,
        string vahedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default);
}
