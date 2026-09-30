using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class TreasuryBankStatementLineRepository : ITreasuryBankStatementLineRepository
{
    private readonly LegacyDbContext _dbContext;

    public TreasuryBankStatementLineRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_TR_BANK_STATEMENT_LINE line, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TR_BANK_STATEMENT_LINEs.AddAsync(line, cancellationToken);
    }

    public async Task<TB_TR_BANK_STATEMENT_LINE?> GetForUpdateAsync(
        Guid id, Guid statementId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_TR_BANK_STATEMENT_LINEs
            .FirstOrDefaultAsync(l => l.ID == id && l.STATEMENT_ID == statementId, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_TR_BANK_STATEMENT_LINE>> GetActiveByStatementAsync(
        Guid statementId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_TR_BANK_STATEMENT_LINEs
            .Where(l => l.STATEMENT_ID == statementId && !l.ISDELETED)
            .OrderBy(l => l.LINE_DATE)
            .ThenBy(l => l.ID)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Guid>> GetMatchedVoucherDetailIdsAsync(
        string vahedCode, CancellationToken cancellationToken = default)
    {
        var ids = await (
            from line in _dbContext.TB_TR_BANK_STATEMENT_LINEs.AsNoTracking()
            join statement in _dbContext.TB_TR_BANK_STATEMENTs.AsNoTracking() on line.STATEMENT_ID equals statement.ID
            where !line.ISDELETED && !statement.ISDELETED && statement.VAHEDCODE == vahedCode
                  && line.MATCHED_VOUCHERDETAIL_ID != null
            select line.MATCHED_VOUCHERDETAIL_ID!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return ids;
    }

    public async Task<int> SoftDeleteByStatementAsync(
        Guid statementId, string? changeUserId, DateTime updatedDate, CancellationToken cancellationToken = default)
    {
        var lines = await _dbContext.TB_TR_BANK_STATEMENT_LINEs
            .Where(l => l.STATEMENT_ID == statementId && !l.ISDELETED)
            .ToListAsync(cancellationToken);

        foreach (var line in lines)
        {
            line.ISDELETED = true;
            line.CHANGEUSERID = changeUserId;
            line.UPDATEDDATE = updatedDate;
        }

        return lines.Count;
    }
}
