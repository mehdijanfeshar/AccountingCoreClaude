using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Queries;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class PettyCashDocEventReadRepository : IPettyCashDocEventReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public PettyCashDocEventReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<IReadOnlyList<PettyCashDocEventDto>> GetByExpenseDocIdAsync(
        Guid expenseDocId,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        // Ownership of the parent document is verified by the caller (GetPettyCashDocEventsQueryHandler)
        // before this is invoked — this method itself simply scopes by the document id.
        return QueryAsync(expenseDocId, cancellationToken);
    }

    private async Task<IReadOnlyList<PettyCashDocEventDto>> QueryAsync(Guid expenseDocId, CancellationToken cancellationToken)
    {
        var events = await _dbContext.TB_PC_DOC_EVENTs
            .AsNoTracking()
            .Where(e => e.EXPENSE_DOC_ID == expenseDocId)
            .OrderBy(e => e.CREATEDDATE)
            .ThenBy(e => e.ID)
            .Select(e => new PettyCashDocEventDto(
                e.ID,
                e.ACTION,
                e.FROM_STATE,
                e.TO_STATE,
                e.NOTE,
                e.RETURN_REASONS,
                e.ADDUSERID,
                e.CREATEDDATE,
                e.CLIENT_IP))
            .ToListAsync(cancellationToken);

        return events;
    }

    public async Task<PettyCashDocEventDto?> GetLastReturnEventAsync(
        Guid expenseDocId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_PC_DOC_EVENTs
            .AsNoTracking()
            .Where(e => e.EXPENSE_DOC_ID == expenseDocId && e.ACTION == PettyCashDocAction.Return)
            .OrderByDescending(e => e.CREATEDDATE)
            .ThenByDescending(e => e.ID)
            .Select(e => new PettyCashDocEventDto(
                e.ID,
                e.ACTION,
                e.FROM_STATE,
                e.TO_STATE,
                e.NOTE,
                e.RETURN_REASONS,
                e.ADDUSERID,
                e.CREATEDDATE,
                e.CLIENT_IP))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
