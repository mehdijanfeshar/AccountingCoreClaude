using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class FsNarrativeRepository : IFsNarrativeRepository
{
    private readonly LegacyDbContext _dbContext;

    public FsNarrativeRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TB_FS_NARRATIVE>> GetSetAsync(
        string vahedCode, FsFramework framework, string year, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_FS_NARRATIVEs.Where(n => n.VAHEDCODE == vahedCode && n.FRAMEWORK == framework && n.YEAR == year && !n.ISDELETED);

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        return await query.OrderBy(n => n.ORDER_NO).ToListAsync(cancellationToken);
    }

    public Task<TB_FS_NARRATIVE?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.TB_FS_NARRATIVEs.FirstOrDefaultAsync(n => n.ID == id && !n.ISDELETED, cancellationToken);

    public async Task AddAsync(TB_FS_NARRATIVE narrative, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_NARRATIVEs.AddAsync(narrative, cancellationToken);
    }

    public async Task AddVersionAsync(TB_FS_NARRATIVE_VERSION version, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_NARRATIVE_VERSIONs.AddAsync(version, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_FS_NARRATIVE_VERSION>> GetVersionsAsync(Guid narrativeId, CancellationToken cancellationToken = default)
        => await _dbContext.TB_FS_NARRATIVE_VERSIONs
            .AsNoTracking()
            .Where(v => v.NARRATIVE_ID == narrativeId)
            .OrderByDescending(v => v.VERSION_NO)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TB_FS_RUN_NARRATIVE>> GetRunNarrativesAsync(Guid runId, string vahedCode, CancellationToken cancellationToken = default)
        => await _dbContext.TB_FS_RUN_NARRATIVEs
            .AsNoTracking()
            .Where(n => n.RUN_ID == runId && n.VAHEDCODE == vahedCode)
            .OrderBy(n => n.ORDER_NO)
            .ToListAsync(cancellationToken);

    public async Task AddRunNarrativesAsync(IEnumerable<TB_FS_RUN_NARRATIVE> rows, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_RUN_NARRATIVEs.AddRangeAsync(rows, cancellationToken);
    }
}
