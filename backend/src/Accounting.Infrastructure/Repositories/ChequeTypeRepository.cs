using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IChequeTypeRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class ChequeTypeRepository : IChequeTypeRepository
{
    private readonly LegacyDbContext _dbContext;

    public ChequeTypeRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_CHECK_TYPE chequeType, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_CHECK_TYPEs.AddAsync(chequeType, cancellationToken);
    }

    public async Task<TB_CHECK_TYPE?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_CHECK_TYPEs
            .FirstOrDefaultAsync(c => c.ID == id, cancellationToken);
    }
}
