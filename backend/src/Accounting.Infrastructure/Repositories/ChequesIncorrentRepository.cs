using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IChequesIncorrentRepository"/>. Only stages
/// the entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never
/// calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class ChequesIncorrentRepository : IChequesIncorrentRepository
{
    private readonly LegacyDbContext _dbContext;

    public ChequesIncorrentRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_CHEQUES_INCORRENT chequesIncorrent, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_CHEQUES_INCORRENTs.AddAsync(chequesIncorrent, cancellationToken);
    }

    public async Task<TB_CHEQUES_INCORRENT?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_CHEQUES_INCORRENTs
            .FirstOrDefaultAsync(c => c.ID == id, cancellationToken);
    }
}
