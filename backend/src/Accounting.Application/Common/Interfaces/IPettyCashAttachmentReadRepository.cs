using Accounting.Application.PettyCash.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for <c>TB_PC_ATTACHMENT</c> — backs
/// <c>GET api/petty-cash/expense-docs/{id}/attachments</c> (metadata only, never
/// <c>ATTACH_FILE</c>) and its <c>.../download</c> sibling (single row, with bytes).
/// <c>AsNoTracking()</c>, like every other petty-cash read repository.
/// </summary>
public interface IPettyCashAttachmentReadRepository
{
    /// <summary>
    /// Every active (<c>ISDELETED == false</c>) attachment's metadata for
    /// <paramref name="expenseDocId"/>, ordered by <c>ATTACH_RADIF</c> — never includes
    /// <c>ATTACH_FILE</c>. Returns an empty list — never <see langword="null"/> — when there are
    /// none.
    /// </summary>
    Task<IReadOnlyList<PettyCashAttachmentDto>> GetByExpenseDocIdAsync(
        Guid expenseDocId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A single attachment's bytes for download, or <see langword="null"/> when
    /// <paramref name="id"/> does not exist, is soft-deleted, or does not belong to
    /// <paramref name="expenseDocId"/>.
    /// </summary>
    Task<PettyCashAttachmentFileDto?> GetFileAsync(
        Guid expenseDocId,
        Guid id,
        CancellationToken cancellationToken = default);
}
