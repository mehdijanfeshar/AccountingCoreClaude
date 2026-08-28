using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IIdentityGroupRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class IdentityGroupRepository : IIdentityGroupRepository
{
    private readonly LegacyDbContext _dbContext;

    public IdentityGroupRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_IDENTITYGROUP identityGroup, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_IDENTITYGROUPs.AddAsync(identityGroup, cancellationToken);
    }

    public async Task<TB_IDENTITYGROUP?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_IDENTITYGROUPs
            .FirstOrDefaultAsync(g => g.ID == id, cancellationToken);
    }
}
