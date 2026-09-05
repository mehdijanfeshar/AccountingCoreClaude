using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IReceiptRepository"/>. Only stages the entity
/// via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class ReceiptRepository : IReceiptRepository
{
    private readonly LegacyDbContext _dbContext;

    public ReceiptRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_RECEIP receipt, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_RECEIPs.AddAsync(receipt, cancellationToken);
    }

    public async Task<TB_RECEIP?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_RECEIPs
            .FirstOrDefaultAsync(r => r.ID == id, cancellationToken);
    }
}
