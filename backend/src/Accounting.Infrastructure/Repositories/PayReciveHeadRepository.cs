using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPayReciveHeadRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
///
/// ⚠️ HEAD ONLY — no method here touches <c>TB_PAYRECIVDETAIL</c>.
/// </summary>
public sealed class PayReciveHeadRepository : IPayReciveHeadRepository
{
    private readonly LegacyDbContext _dbContext;

    public PayReciveHeadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PAYRECIVHEAD payReciveHead, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PAYRECIVHEADs.AddAsync(payReciveHead, cancellationToken);
    }

    public async Task<TB_PAYRECIVHEAD?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_PAYRECIVHEADs
            .FirstOrDefaultAsync(e => e.ID == id, cancellationToken);
    }
}
