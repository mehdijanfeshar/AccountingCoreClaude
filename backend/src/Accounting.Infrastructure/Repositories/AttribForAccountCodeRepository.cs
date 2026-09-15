using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IAttribForAccountCodeRepository"/>. Only stages
/// the entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never
/// calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class AttribForAccountCodeRepository : IAttribForAccountCodeRepository
{
    private readonly LegacyDbContext _dbContext;

    public AttribForAccountCodeRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_ATTRIBFORACCOUNTCODE attribForAccountCode, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_ATTRIBFORACCOUNTCODEs.AddAsync(attribForAccountCode, cancellationToken);
    }

    public async Task<TB_ATTRIBFORACCOUNTCODE?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_ATTRIBFORACCOUNTCODEs
            .FirstOrDefaultAsync(a => a.ID == id, cancellationToken);
    }
}
