using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IRevolvingFundRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never calls
/// SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class RevolvingFundRepository : IRevolvingFundRepository
{
    private readonly LegacyDbContext _dbContext;

    public RevolvingFundRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_REVOLVING_FUND revolvingFund, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_REVOLVING_FUNDs.AddAsync(revolvingFund, cancellationToken);
    }

    public async Task<TB_REVOLVING_FUND?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_REVOLVING_FUNDs
            .FirstOrDefaultAsync(r => r.ID == id, cancellationToken);
    }
}
