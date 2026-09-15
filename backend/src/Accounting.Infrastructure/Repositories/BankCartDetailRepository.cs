using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IBankCartDetailRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class BankCartDetailRepository : IBankCartDetailRepository
{
    private readonly LegacyDbContext _dbContext;

    public BankCartDetailRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_BANKCARTDETAIL bankCartDetail, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_BANKCARTDETAILs.AddAsync(bankCartDetail, cancellationToken);
    }

    public async Task<TB_BANKCARTDETAIL?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_BANKCARTDETAILs
            .FirstOrDefaultAsync(b => b.ID == id, cancellationToken);
    }
}
