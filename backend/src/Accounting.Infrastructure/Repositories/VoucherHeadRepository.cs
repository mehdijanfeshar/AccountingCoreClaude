using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IVoucherHeadRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never
/// calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class VoucherHeadRepository : IVoucherHeadRepository
{
    private readonly LegacyDbContext _dbContext;

    public VoucherHeadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_VOUCHERSHEAD voucherHead, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_VOUCHERSHEADs.AddAsync(voucherHead, cancellationToken);
    }

    public async Task<TB_VOUCHERSHEAD?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_VOUCHERSHEADs
            .FirstOrDefaultAsync(h => h.ID == id, cancellationToken);
    }
}
