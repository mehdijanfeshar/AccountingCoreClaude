using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IWorkShopRepository"/>. Only stages the entity
/// via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class WorkShopRepository : IWorkShopRepository
{
    private readonly LegacyDbContext _dbContext;

    public WorkShopRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_WORKSHOP workShop, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_WORKSHOPs.AddAsync(workShop, cancellationToken);
    }

    public async Task<TB_WORKSHOP?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_WORKSHOPs
            .FirstOrDefaultAsync(w => w.ID == id, cancellationToken);
    }

    public async Task AddTafsiliLinkAsync(TB_WORKSHOP_LINK_TAFSILI link, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_WORKSHOP_LINK_TAFSILIs.AddAsync(link, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_WORKSHOP_LINK_TAFSILI>> GetActiveTafsiliLinksAsync(
        Guid workShopId,
        CancellationToken cancellationToken = default)
    {
        // Change-tracked on purpose (no AsNoTracking): the update handler soft-deletes dropped
        // links by mutating these instances in place. ISDELETED is non-nullable here, so
        // "== false" is the complete "active" predicate.
        return await _dbContext.TB_WORKSHOP_LINK_TAFSILIs
            .Where(l => l.WORKSHOP_ID == workShopId && l.ISDELETED == false)
            .ToListAsync(cancellationToken);
    }
}
