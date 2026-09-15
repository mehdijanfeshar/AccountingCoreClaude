using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IWhiteAndBlackListRepository"/>. Only stages
/// the entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never
/// calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class WhiteAndBlackListRepository : IWhiteAndBlackListRepository
{
    private readonly LegacyDbContext _dbContext;

    public WhiteAndBlackListRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_WHITEANDBLACKLIST whiteAndBlackList, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_WHITEANDBLACKLISTs.AddAsync(whiteAndBlackList, cancellationToken);
    }

    public async Task<TB_WHITEANDBLACKLIST?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_WHITEANDBLACKLISTs
            .FirstOrDefaultAsync(w => w.ID == id, cancellationToken);
    }
}
