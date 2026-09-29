using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class TreasuryRoleRepository : ITreasuryRoleRepository
{
    private readonly LegacyDbContext _dbContext;

    public TreasuryRoleRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_TR_ROLE role, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TR_ROLEs.AddAsync(role, cancellationToken);
    }

    public async Task<TB_TR_ROLE?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_TR_ROLEs.FirstOrDefaultAsync(r => r.ID == id, cancellationToken);

        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "TreasuryRole");

        return entity;
    }

    public Task<TB_TR_ROLE?> GetByVahedUserAndRoleAsync(
        string vahedCode, string userId, TreasuryRole role, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_TR_ROLEs
            .FirstOrDefaultAsync(r => r.VAHEDCODE == vahedCode && r.USERID == userId && r.ROLE == role, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TreasuryRole>> GetActiveRolesAsync(
        string vahedCode, string userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_TR_ROLEs
            .AsNoTracking()
            .Where(r => r.VAHEDCODE == vahedCode && r.USERID == userId && !r.ISDELETED)
            .Select(r => r.ROLE)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveFinanceManagerAsync(string vahedCode, CancellationToken cancellationToken = default)
    {
        var count = await _dbContext.TB_TR_ROLEs
            .AsNoTracking()
            .Where(r => r.VAHEDCODE == vahedCode && !r.ISDELETED && r.ROLE == TreasuryRole.FinanceManager)
            .CountAsync(cancellationToken);

        return count > 0;
    }
}
