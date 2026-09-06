using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="ITmpVoucherHeadRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
///
/// ⚠️ HEAD ONLY — no method here touches <c>TB_TMP_VOUCHERSDETAIL</c>.
/// </summary>
public sealed class TmpVoucherHeadRepository : ITmpVoucherHeadRepository
{
    private readonly LegacyDbContext _dbContext;

    public TmpVoucherHeadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_TMP_VOUCHERHEAD tmpVoucherHead, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TMP_VOUCHERHEADs.AddAsync(tmpVoucherHead, cancellationToken);
    }

    public async Task<TB_TMP_VOUCHERHEAD?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_TMP_VOUCHERHEADs
            .FirstOrDefaultAsync(e => e.ID == id, cancellationToken);
    }
}
