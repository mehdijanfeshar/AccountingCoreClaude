using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ICheckBookRepository"/>. Only stages the entity
/// via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class CheckBookRepository : ICheckBookRepository
{
    private readonly LegacyDbContext _dbContext;

    public CheckBookRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_CHECKBOOK checkBook, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_CHECKBOOKs.AddAsync(checkBook, cancellationToken);
    }

    public async Task<TB_CHECKBOOK?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_CHECKBOOKs
            .FirstOrDefaultAsync(c => c.ID == id, cancellationToken);
    }
}
