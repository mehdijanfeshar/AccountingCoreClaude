using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ILevelTafsilRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class LevelTafsilRepository : ILevelTafsilRepository
{
    private readonly LegacyDbContext _dbContext;

    public LevelTafsilRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_LEVEL_TAFSIL levelTafsil, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_LEVEL_TAFSILs.AddAsync(levelTafsil, cancellationToken);
    }

    public async Task<TB_LEVEL_TAFSIL?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_LEVEL_TAFSILs
            .FirstOrDefaultAsync(l => l.ID == id, cancellationToken);
    }
}
