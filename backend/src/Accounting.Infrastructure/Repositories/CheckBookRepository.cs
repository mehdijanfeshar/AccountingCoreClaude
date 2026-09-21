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

    public async Task<TB_CHECKBOOK?> GetForUpdateAsync(
        Guid id,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_CHECKBOOKs
            .FirstOrDefaultAsync(c => c.ID == id, cancellationToken);

        // Fetched by ID alone, then judged — a WHERE on VAHEDCODE could not tell "no such row"
        // apart from "another unit's row", and those answer 404 and 403 respectively.
        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "CheckBook");

        return entity;
    }
}
