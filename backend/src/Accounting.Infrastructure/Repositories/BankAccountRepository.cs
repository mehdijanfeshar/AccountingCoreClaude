using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IBankAccountRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class BankAccountRepository : IBankAccountRepository
{
    private readonly LegacyDbContext _dbContext;

    public BankAccountRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_ACCOUNT bankAccount, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_ACCOUNTs.AddAsync(bankAccount, cancellationToken);
    }

    public async Task<TB_ACCOUNT?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_ACCOUNTs
            .FirstOrDefaultAsync(a => a.ID == id, cancellationToken);
    }
}
