using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_ACCOUNT"/> (a bank account — NOT the chart-of-accounts
/// node <c>TB_ACCOUNTCODE</c>). Only stages changes — it does NOT call SaveChanges; the handler
/// owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IBankAccountRepository
{
    Task AddAsync(TB_ACCOUNT bankAccount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_ACCOUNT"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>, unlike the read repository) so that
    /// Update/Delete handlers can mutate the returned instance in place and have EF Core
    /// generate the correct UPDATE on <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns
    /// <see langword="null"/> when no row with that <c>ID</c> exists — soft-deleted rows are
    /// still returned here (the caller decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_ACCOUNT?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new <see cref="TB_ACCOUNT_LINK_TAFSILI"/> row for insert as part of its parent
    /// bank account's write. Only stages — the handler still owns the single
    /// <see cref="IUnitOfWork.SaveChangesAsync"/>, so an account and its تفصیلی links are always
    /// persisted atomically.
    ///
    /// <b>Deliberately named <c>AddTafsiliLinkAsync</c>, not <c>AddAsync</c>.</b> Same reasoning
    /// as <see cref="IVoucherDetailRepository.AddTafsiliLinkAsync"/>: an aggregate-root-shaped
    /// <c>AddAsync</c> taking a <c>*_LINK_TAFSIL*</c> entity would promote this permanently
    /// embedded table to an aggregate root of its own, which
    /// <c>NoIndependentLinkTableWritePathTests</c> forbids. There is deliberately no
    /// link-table repository, controller or MediatR request.
    /// </summary>
    Task AddTafsiliLinkAsync(TB_ACCOUNT_LINK_TAFSILI link, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the account's currently-active (<c>ISDELETED == false</c>) تفصیلی links as
    /// change-tracked entities (deliberately no <c>AsNoTracking()</c>), so
    /// <c>UpdateBankAccountCommandHandler</c> can reconcile the caller's requested set against
    /// them and soft-delete the dropped ones in place, inside the handler's single
    /// <see cref="IUnitOfWork.SaveChangesAsync"/>.
    ///
    /// Returns an empty list — never <see langword="null"/> — when the account has no links.
    /// </summary>
    Task<IReadOnlyList<TB_ACCOUNT_LINK_TAFSILI>> GetActiveTafsiliLinksAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);
}
