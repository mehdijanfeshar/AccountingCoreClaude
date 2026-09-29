using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_TR_TRANSFER"/> ("انتقال وجه") — خزانه‌داری، بخش ۴-ج
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Only stages changes — never calls SaveChanges;
/// the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface ITreasuryTransferRepository
{
    Task AddAsync(TB_TR_TRANSFER transfer, CancellationToken cancellationToken = default);

    /// <summary>Loads a single transfer by <c>ID</c>, change-tracked, scoped to
    /// <paramref name="vahedCode"/> via <c>VahedOwnership</c>. Returns <see langword="null"/> when
    /// no row with that <c>ID</c> exists.</summary>
    Task<TB_TR_TRANSFER?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Next value of the per-(VAHEDCODE, YEAR) 6-digit counter behind <c>CODE</c> ("TRF-" + this
    /// value, zero-padded).
    /// </summary>
    Task<int> GetNextCodeAsync(string vahedCode, string year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sum of <c>AMOUNT</c> over every non-deleted <see cref="Accounting.Domain.ValueObjects.TransferState.Executed"/>
    /// transfer from <paramref name="sourceBankAccountId"/> on <paramref name="transferDate"/> —
    /// blocking control (b) of <c>approve</c> (daily transfer cap). <paramref name="excludeId"/>
    /// excludes the transfer being approved itself (relevant only if it were somehow already
    /// Executed, defensive).
    /// </summary>
    Task<decimal> GetExecutedAmountForSourceOnDateAsync(
        Guid sourceBankAccountId,
        string transferDate,
        Guid? excludeId,
        CancellationToken cancellationToken = default);
}
