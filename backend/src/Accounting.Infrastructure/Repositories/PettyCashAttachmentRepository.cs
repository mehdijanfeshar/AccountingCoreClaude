using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class PettyCashAttachmentRepository : IPettyCashAttachmentRepository
{
    private readonly LegacyDbContext _dbContext;

    public PettyCashAttachmentRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PC_ATTACHMENT attachment, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PC_ATTACHMENTs.AddAsync(attachment, cancellationToken);
    }

    public async Task<TB_PC_ATTACHMENT?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_PC_ATTACHMENTs
            .FirstOrDefaultAsync(a => a.ID == id, cancellationToken);

        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "PettyCashAttachment");

        return entity;
    }

    public async Task<int> GetMaxRadifAsync(Guid expenseDocId, CancellationToken cancellationToken = default)
    {
        // CountAsync, not AnyAsync — see PettyCashExpenseDocRepository.ExistsActiveDuplicateAsync (ORA-00904 on pre-23ai Oracle).
        var hasAny = await _dbContext.TB_PC_ATTACHMENTs
            .AsNoTracking()
            .CountAsync(a => a.EXPENSE_DOC_ID == expenseDocId && !a.ISDELETED, cancellationToken) > 0;

        if (!hasAny)
        {
            return 0;
        }

        return await _dbContext.TB_PC_ATTACHMENTs
            .AsNoTracking()
            .Where(a => a.EXPENSE_DOC_ID == expenseDocId && !a.ISDELETED)
            .MaxAsync(a => a.ATTACH_RADIF, cancellationToken);
    }
}
