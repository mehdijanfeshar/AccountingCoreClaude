using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Queries;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPettyCashRefundReadRepository"/>. Reads
/// directly from <see cref="LegacyDbContext"/> with <c>AsNoTracking()</c> — same deliberate,
/// narrow exception as every other petty-cash read repository.
/// </summary>
public sealed class PettyCashRefundReadRepository : IPettyCashRefundReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public PettyCashRefundReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PettyCashRefundDto>> GetByFundAsync(
        Guid fundId, string vahedCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_PC_REFUNDs
            .AsNoTracking()
            .Where(r => r.FUND_ID == fundId && r.VAHEDCODE == vahedCode && !r.ISDELETED)
            .OrderByDescending(r => r.CREATEDDATE)
            .ThenByDescending(r => r.ID)
            .Select(r => new PettyCashRefundDto(
                r.ID, r.FUND_ID, r.CODE, r.AMOUNT, r.REASON, r.REFUND_DATE, r.RECORDED_BY_USERID, r.CREATEDDATE))
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalAsync(Guid fundId, string vahedCode, CancellationToken cancellationToken = default)
    {
        var amounts = await _dbContext.TB_PC_REFUNDs
            .AsNoTracking()
            .Where(r => r.FUND_ID == fundId && r.VAHEDCODE == vahedCode && !r.ISDELETED)
            .Select(r => r.AMOUNT)
            .ToListAsync(cancellationToken);

        return amounts.Sum();
    }
}
