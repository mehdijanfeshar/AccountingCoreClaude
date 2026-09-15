using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IBillLogRepository"/>. Only stages the entity
/// — never calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class BillLogRepository : IBillLogRepository
{
    private readonly LegacyDbContext _dbContext;

    public BillLogRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_BILL_LOG billLog, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_BILL_LOGs.AddAsync(billLog, cancellationToken);
    }

    public async Task<TB_BILL_LOG?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_BILL_LOGs
            .FirstOrDefaultAsync(b => b.ID == id, cancellationToken);
    }
}
