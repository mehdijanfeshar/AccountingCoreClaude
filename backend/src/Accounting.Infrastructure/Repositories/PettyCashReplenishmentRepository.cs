using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPettyCashReplenishmentRepository"/>. Only
/// stages changes — never calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="IUnitOfWork"/>.
/// </summary>
public sealed class PettyCashReplenishmentRepository : IPettyCashReplenishmentRepository
{
    private readonly LegacyDbContext _dbContext;

    public PettyCashReplenishmentRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PC_REPLENISHMENT replenishment, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PC_REPLENISHMENTs.AddAsync(replenishment, cancellationToken);
    }

    public async Task<TB_PC_REPLENISHMENT?> GetForUpdateAsync(
        Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_PC_REPLENISHMENTs
            .FirstOrDefaultAsync(r => r.ID == id, cancellationToken);

        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "PettyCashReplenishment");

        return entity;
    }

    public async Task<decimal> GetPaidTotalAsync(Guid fundId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var amounts = await _dbContext.TB_PC_REPLENISHMENTs
            .AsNoTracking()
            .Where(r => r.FUND_ID == fundId && r.VAHEDCODE == vahedCode && !r.ISDELETED
                        && r.STATE == PettyCashReplenishmentState.Paid)
            .Select(r => r.TOTAL_AMOUNT)
            .ToListAsync(cancellationToken);

        return amounts.Sum();
    }
}
