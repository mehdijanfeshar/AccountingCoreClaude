using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Queries;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class PettyCashAttachmentReadRepository : IPettyCashAttachmentReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public PettyCashAttachmentReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PettyCashAttachmentDto>> GetByExpenseDocIdAsync(
        Guid expenseDocId,
        CancellationToken cancellationToken = default)
    {
        // Ownership of the parent document is verified by the caller
        // (GetPettyCashAttachmentsQueryHandler) before this is invoked. Never selects
        // ATTACH_FILE — metadata only (CLAUDE.md rule 6 / this module's own decision doc).
        return await _dbContext.TB_PC_ATTACHMENTs
            .AsNoTracking()
            .Where(a => a.EXPENSE_DOC_ID == expenseDocId && !a.ISDELETED)
            .OrderBy(a => a.ATTACH_RADIF)
            .Select(a => new PettyCashAttachmentDto(
                a.ID,
                a.EXPENSE_DOC_ID,
                a.ATTACH_NAME,
                a.ATTACH_SIZE,
                a.CONTENT_TYPE,
                a.ATTACH_RADIF,
                a.ADDUSERID,
                a.CREATEDDATE))
            .ToListAsync(cancellationToken);
    }

    public async Task<PettyCashAttachmentFileDto?> GetFileAsync(
        Guid expenseDocId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Ownership of the parent document is verified by the caller
        // (GetPettyCashAttachmentFileQueryHandler) before this is invoked.
        return await _dbContext.TB_PC_ATTACHMENTs
            .AsNoTracking()
            .Where(a => a.ID == id && a.EXPENSE_DOC_ID == expenseDocId && !a.ISDELETED)
            .Select(a => new PettyCashAttachmentFileDto(a.ID, a.ATTACH_NAME, a.CONTENT_TYPE, a.ATTACH_FILE))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
