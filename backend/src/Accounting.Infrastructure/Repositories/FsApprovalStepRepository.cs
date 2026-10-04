using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class FsApprovalStepRepository : IFsApprovalStepRepository
{
    private readonly LegacyDbContext _dbContext;

    public FsApprovalStepRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TB_FS_APPROVAL_STEP>> GetAllAsync(FsFramework? framework, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_FS_APPROVAL_STEPs.AsNoTracking().Where(s => !s.ISDELETED);

        if (framework is { } fw)
        {
            query = query.Where(s => s.FRAMEWORK == fw);
        }

        return await query.OrderBy(s => s.FRAMEWORK).ThenBy(s => s.STEP_NO).ToListAsync(cancellationToken);
    }

    public Task<TB_FS_APPROVAL_STEP?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.TB_FS_APPROVAL_STEPs.FirstOrDefaultAsync(s => s.ID == id && !s.ISDELETED, cancellationToken);

    public async Task AddAsync(TB_FS_APPROVAL_STEP step, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_APPROVAL_STEPs.AddAsync(step, cancellationToken);
    }
}
