using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_CHECKBOOK"/>. Only stages changes — it does NOT call
/// SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface ICheckBookRepository
{
    Task AddAsync(TB_CHECKBOOK checkBook, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_CHECKBOOK"/> by <c>ID</c> as a change-tracked entity
    /// (deliberately no <c>AsNoTracking()</c>, unlike the read repository) so that
    /// Update/Delete handlers can mutate the returned instance in place and have EF Core
    /// generate the correct UPDATE on <see cref="IUnitOfWork.SaveChangesAsync"/>. Returns
    /// <see langword="null"/> when no row with that <c>ID</c> exists — soft-deleted rows are
    /// still returned here (the caller decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_CHECKBOOK?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>اوراق دسته‌چک (tracked) — جزء همان Aggregate. پیش‌فرض فقط فعال‌ها.</summary>
    Task<IReadOnlyList<TB_CHECK>> GetLeavesForUpdateAsync(Guid checkBookId, CancellationToken cancellationToken = default, bool includeDeleted = false);

    /// <summary>شمار اوراق این دسته‌چک که در ردیف سند فعال به کار رفته‌اند.</summary>
    Task<int> CountLeavesInVouchersAsync(Guid checkBookId, CancellationToken cancellationToken = default);

    /// <summary>بزرگ‌ترین شمارهٔ برگ این دسته‌چک، حتی حذف‌شده (<c>UK_CHECK</c> حذف‌شده‌ها را هم می‌شمارد)؛ null = بی‌برگ.</summary>
    Task<string?> GetMaxChequeNoAsync(Guid checkBookId, CancellationToken cancellationToken = default);

    /// <summary>دسته‌چک هم‌کلید <c>UK_CHECKBOOK</c> (حساب + اولین + آخرین + واحد)، حتی حذف‌شده — tracked.</summary>
    Task<TB_CHECKBOOK?> FindSameRangeForUpdateAsync(Guid accountId, string from, string to, string vahedCode, CancellationToken cancellationToken = default);
}
