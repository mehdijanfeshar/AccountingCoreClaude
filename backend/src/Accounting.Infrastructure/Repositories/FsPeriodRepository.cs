using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class FsPeriodRepository : IFsPeriodRepository
{
    private readonly LegacyDbContext _dbContext;

    public FsPeriodRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TB_FS_PERIOD>> GetForYearAsync(string year, CancellationToken cancellationToken = default)
        => await _dbContext.TB_FS_PERIODs.AsNoTracking().Where(p => p.YEAR == year).ToListAsync(cancellationToken);

    public Task<TB_FS_PERIOD?> GetForUpdateAsync(string vahedCode, string year, CancellationToken cancellationToken = default)
        => _dbContext.TB_FS_PERIODs.FirstOrDefaultAsync(p => p.VAHEDCODE == vahedCode && p.YEAR == year, cancellationToken);

    public async Task AddAsync(TB_FS_PERIOD period, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_PERIODs.AddAsync(period, cancellationToken);
    }

    public async Task AddLogAsync(TB_FS_PERIOD_LOG log, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_PERIOD_LOGs.AddAsync(log, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_FS_PERIOD_LOG>> GetLogsAsync(string vahedCode, string year, CancellationToken cancellationToken = default)
        => await _dbContext.TB_FS_PERIOD_LOGs
            .AsNoTracking()
            .Where(l => l.VAHEDCODE == vahedCode && l.YEAR == year)
            .OrderByDescending(l => l.CREATEDDATE)
            .ToListAsync(cancellationToken);
}
