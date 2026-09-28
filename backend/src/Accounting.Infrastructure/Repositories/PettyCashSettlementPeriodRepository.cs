using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPettyCashSettlementPeriodRepository"/>. Only
/// stages changes — never calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class PettyCashSettlementPeriodRepository : IPettyCashSettlementPeriodRepository
{
    private readonly LegacyDbContext _dbContext;

    public PettyCashSettlementPeriodRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PC_SETTLEMENT_PERIOD period, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PC_SETTLEMENT_PERIODs.AddAsync(period, cancellationToken);
    }

    public async Task<TB_PC_SETTLEMENT_PERIOD?> GetForUpdateAsync(
        Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_PC_SETTLEMENT_PERIODs
            .FirstOrDefaultAsync(p => p.ID == id, cancellationToken);

        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "PettyCashSettlementPeriod");

        return entity;
    }

    public async Task<TB_PC_SETTLEMENT_PERIOD?> GetDraftAsync(
        Guid fundId, string vahedCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_PC_SETTLEMENT_PERIODs
            .Where(p => p.FUND_ID == fundId && p.VAHEDCODE == vahedCode && !p.ISDELETED
                        && p.STATE == PettyCashSettlementState.Draft)
            .OrderByDescending(p => p.CREATEDDATE)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TB_PC_SETTLEMENT_PERIOD?> GetLatestFinalAsync(
        Guid fundId, string vahedCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_PC_SETTLEMENT_PERIODs
            .Where(p => p.FUND_ID == fundId && p.VAHEDCODE == vahedCode && !p.ISDELETED
                        && p.STATE == PettyCashSettlementState.Final)
            .OrderByDescending(p => p.PERIOD_END)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsFinalCoveringDateAsync(
        Guid fundId, string date, string vahedCode, CancellationToken cancellationToken = default)
    {
        // CountAsync, not AnyAsync: the Oracle provider renders AnyAsync as
        // "CASE WHEN EXISTS ... THEN True ELSE False", which pre-23ai Oracle rejects (ORA-00904).
        return await _dbContext.TB_PC_SETTLEMENT_PERIODs
            .AsNoTracking()
            .Where(p => p.FUND_ID == fundId && p.VAHEDCODE == vahedCode && !p.ISDELETED
                        && p.STATE == PettyCashSettlementState.Final
                        && string.Compare(p.PERIOD_START, date) <= 0
                        && string.Compare(p.PERIOD_END, date) >= 0)
            .CountAsync(cancellationToken) > 0;
    }
}
