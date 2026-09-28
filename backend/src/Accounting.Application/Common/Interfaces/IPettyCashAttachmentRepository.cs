using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_PC_ATTACHMENT"/>. Only stages changes — never calls
/// SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface IPettyCashAttachmentRepository
{
    Task AddAsync(TB_PC_ATTACHMENT attachment, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a single <see cref="TB_PC_ATTACHMENT"/> by <c>ID</c> as change-tracked, verifying
    /// unit ownership via <c>VahedOwnership</c> like every other by-id repository method in this
    /// project. Returns <see langword="null"/> when no row with that <c>ID</c> exists —
    /// soft-deleted rows are still returned (the caller decides how to treat <c>ISDELETED</c>).
    /// </summary>
    Task<TB_PC_ATTACHMENT?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// The current maximum <c>ATTACH_RADIF</c> among non-deleted attachments of
    /// <paramref name="expenseDocId"/>, or 0 when it has none — the caller adds 1 for a new row's
    /// display order.
    /// </summary>
    Task<int> GetMaxRadifAsync(Guid expenseDocId, CancellationToken cancellationToken = default);
}
