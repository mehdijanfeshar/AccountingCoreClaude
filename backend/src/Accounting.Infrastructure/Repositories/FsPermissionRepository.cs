using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class FsPermissionRepository : IFsPermissionRepository
{
    private readonly LegacyDbContext _dbContext;

    public FsPermissionRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TB_FS_PERMISSION>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbContext.TB_FS_PERMISSIONs.AsNoTracking().Where(p => !p.ISDELETED).ToListAsync(cancellationToken);

    public Task<TB_FS_PERMISSION?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.TB_FS_PERMISSIONs.FirstOrDefaultAsync(p => p.ID == id && !p.ISDELETED, cancellationToken);

    public async Task AddAsync(TB_FS_PERMISSION row, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_PERMISSIONs.AddAsync(row, cancellationToken);
    }
}
