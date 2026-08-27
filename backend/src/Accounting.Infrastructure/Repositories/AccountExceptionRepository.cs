using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IAccountExceptionRepository"/>. Only stages
/// the entity — never calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class AccountExceptionRepository : IAccountExceptionRepository
{
    private readonly LegacyDbContext _dbContext;

    public AccountExceptionRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_ACCOUNTEXCEPTION accountException, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_ACCOUNTEXCEPTIONs.AddAsync(accountException, cancellationToken);
    }

    public async Task<TB_ACCOUNTEXCEPTION?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_ACCOUNTEXCEPTIONs
            .FirstOrDefaultAsync(a => a.ID == id, cancellationToken);
    }
}
