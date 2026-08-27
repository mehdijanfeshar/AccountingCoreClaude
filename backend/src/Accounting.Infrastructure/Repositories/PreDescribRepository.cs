using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPreDescribRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never
/// calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class PreDescribRepository : IPreDescribRepository
{
    private readonly LegacyDbContext _dbContext;

    public PreDescribRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PREDESCRIB preDescrib, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PREDESCRIBs.AddAsync(preDescrib, cancellationToken);
    }

    public async Task<TB_PREDESCRIB?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_PREDESCRIBs
            .FirstOrDefaultAsync(p => p.ID == id, cancellationToken);
    }
}
