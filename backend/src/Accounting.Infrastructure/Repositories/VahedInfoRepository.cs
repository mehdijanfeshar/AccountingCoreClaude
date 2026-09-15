using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IVahedInfoRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never
/// calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class VahedInfoRepository : IVahedInfoRepository
{
    private readonly LegacyDbContext _dbContext;

    public VahedInfoRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_VAHED_INFO vahedInfo, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_VAHED_INFOs.AddAsync(vahedInfo, cancellationToken);
    }

    public async Task<TB_VAHED_INFO?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_VAHED_INFOs
            .FirstOrDefaultAsync(v => v.ID == id, cancellationToken);
    }
}
