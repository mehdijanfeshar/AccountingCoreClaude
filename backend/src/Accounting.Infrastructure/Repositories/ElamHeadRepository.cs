using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IElamHeadRepository"/>. Only stages the entity
/// via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
///
/// ⚠️ HEAD ONLY — no method here touches <c>TB_ELAMDETAIL</c>.
/// </summary>
public sealed class ElamHeadRepository : IElamHeadRepository
{
    private readonly LegacyDbContext _dbContext;

    public ElamHeadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_ELAMHEAD elamHead, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_ELAMHEADs.AddAsync(elamHead, cancellationToken);
    }

    public async Task<TB_ELAMHEAD?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_ELAMHEADs
            .FirstOrDefaultAsync(e => e.ID == id, cancellationToken);
    }
}
