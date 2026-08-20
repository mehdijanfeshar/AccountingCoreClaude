using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IAccountCodeRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never
/// calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class AccountCodeRepository : IAccountCodeRepository
{
    private readonly LegacyDbContext _dbContext;

    public AccountCodeRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_ACCOUNTCODE accountCode, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_ACCOUNTCODEs.AddAsync(accountCode, cancellationToken);
    }

    public async Task<TB_ACCOUNTCODE?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_ACCOUNTCODEs
            .FirstOrDefaultAsync(a => a.ID == id, cancellationToken);
    }
}
