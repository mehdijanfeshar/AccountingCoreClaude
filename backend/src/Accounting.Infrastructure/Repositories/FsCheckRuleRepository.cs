using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class FsCheckRuleRepository : IFsCheckRuleRepository
{
    private readonly LegacyDbContext _dbContext;

    public FsCheckRuleRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TB_FS_CHECK_RULE>> GetAllAsync(FsFramework? framework, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TB_FS_CHECK_RULEs.AsNoTracking().Where(r => !r.ISDELETED);

        if (framework is { } fw)
        {
            query = query.Where(r => r.FRAMEWORK == fw);
        }

        return await query.OrderBy(r => r.FRAMEWORK).ThenBy(r => r.CODE).ToListAsync(cancellationToken);
    }

    public Task<TB_FS_CHECK_RULE?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.TB_FS_CHECK_RULEs.FirstOrDefaultAsync(r => r.ID == id && !r.ISDELETED, cancellationToken);

    public async Task<bool> CodeExistsAsync(string? ownerVahedCode, FsFramework framework, string code, CancellationToken cancellationToken = default)
    {
        // CountAsync، نه AnyAsync (ORA-00904).
        var query = _dbContext.TB_FS_CHECK_RULEs.Where(r => r.FRAMEWORK == framework && r.CODE == code);
        query = ownerVahedCode is null ? query.Where(r => r.VAHEDCODE == null) : query.Where(r => r.VAHEDCODE == ownerVahedCode);
        return await query.CountAsync(cancellationToken) > 0;
    }

    public async Task AddAsync(TB_FS_CHECK_RULE rule, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_FS_CHECK_RULEs.AddAsync(rule, cancellationToken);
    }
}
