using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IRabetRepository"/>. Only stages the entity
/// via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class RabetRepository : IRabetRepository
{
    private readonly LegacyDbContext _dbContext;

    public RabetRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_RABET rabet, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_RABETs.AddAsync(rabet, cancellationToken);
    }

    public async Task<TB_RABET?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_RABETs
            .FirstOrDefaultAsync(r => r.ID == id, cancellationToken);
    }
}
