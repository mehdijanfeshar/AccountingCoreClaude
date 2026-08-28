using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ITafsilGroupRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class TafsilGroupRepository : ITafsilGroupRepository
{
    private readonly LegacyDbContext _dbContext;

    public TafsilGroupRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_TAFSIL_GROUP tafsilGroup, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TAFSIL_GROUPs.AddAsync(tafsilGroup, cancellationToken);
    }

    public async Task<TB_TAFSIL_GROUP?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_TAFSIL_GROUPs
            .FirstOrDefaultAsync(t => t.ID == id, cancellationToken);
    }
}
