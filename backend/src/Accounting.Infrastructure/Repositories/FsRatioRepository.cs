using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class FsRatioRepository : IFsRatioRepository
{
    private readonly LegacyDbContext _dbContext;

    public FsRatioRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TB_FS_RATIO>> GetAllAsync(FsFramework? framework, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_FS_RATIOs.AsNoTracking().Where(r => !r.ISDELETED);

        if (framework is { } fw)
        {
            query = query.Where(r => r.FRAMEWORK == fw);
        }

        return await query.OrderBy(r => r.FRAMEWORK).ThenBy(r => r.ORDER_NO).ThenBy(r => r.CODE).ToListAsync(cancellationToken);
    }

    public Task<TB_FS_RATIO?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.TB_FS_RATIOs.FirstOrDefaultAsync(r => r.ID == id && !r.ISDELETED, cancellationToken);

    public async Task AddAsync(TB_FS_RATIO ratio, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_RATIOs.AddAsync(ratio, cancellationToken);
    }
}
