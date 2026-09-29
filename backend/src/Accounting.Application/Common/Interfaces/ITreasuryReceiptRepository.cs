using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_TR_RECEIPT"/> ("دریافت وجه") — خزانه‌داری، بخش ۴-ج
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Only stages changes — never calls SaveChanges;
/// the handler owns the transaction boundary via <see cref="IUnitOfWork"/>. Same shape as
/// <c>IPaymentRequestRepository</c> (بخش ۴-الف), minus the تفصیلی link table (a دریافت has no
/// multi-level مرکز هزینه).
/// </summary>
public interface ITreasuryReceiptRepository
{
    Task AddAsync(TB_TR_RECEIPT receipt, CancellationToken cancellationToken = default);

    /// <summary>Loads a single receipt by <c>ID</c>, change-tracked, scoped to
    /// <paramref name="vahedCode"/> via <c>VahedOwnership</c>. Returns <see langword="null"/> when
    /// no row with that <c>ID</c> exists.</summary>
    Task<TB_TR_RECEIPT?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Next value of the per-(VAHEDCODE, YEAR) 6-digit counter behind <c>CODE</c> ("RCV-" + this
    /// value, zero-padded) — same client-side-parsed-MAX approach as
    /// <c>IPaymentRequestRepository.GetNextCodeAsync</c>.
    /// </summary>
    Task<int> GetNextCodeAsync(string vahedCode, string year, CancellationToken cancellationToken = default);

    /// <summary>
    /// True when at least one other live (non-deleted, non-<see cref="Accounting.Domain.ValueObjects.ReceiptState.Cancelled"/>)
    /// receipt of the same <paramref name="bankAccountId"/> already carries
    /// <paramref name="bankReference"/> — owner decision ۲۰۲۶-۰۹-۲۹, application-level uniqueness
    /// (no DB UNIQUE constraint backs it, same posture as every other such rule in this project).
    /// <paramref name="excludeId"/> is the receipt's own id on Update, so it never matches itself.
    /// Uses <c>CountAsync(...) &gt; 0</c>, never <c>AnyAsync</c>.
    /// </summary>
    Task<bool> ExistsDuplicateBankReferenceAsync(
        Guid bankAccountId,
        string bankReference,
        Guid? excludeId,
        CancellationToken cancellationToken = default);
}
