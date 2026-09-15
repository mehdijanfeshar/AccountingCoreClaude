using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IAccountCodeInterfaceRepository"/>. Only
/// stages the entity — never calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class AccountCodeInterfaceRepository : IAccountCodeInterfaceRepository
{
    private readonly LegacyDbContext _dbContext;

    public AccountCodeInterfaceRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_ACCOUNTCODE_INTERFACE accountCodeInterface, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_ACCOUNTCODE_INTERFACEs.AddAsync(accountCodeInterface, cancellationToken);
    }

    public async Task<TB_ACCOUNTCODE_INTERFACE?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_ACCOUNTCODE_INTERFACEs
            .FirstOrDefaultAsync(a => a.ID == id, cancellationToken);
    }
}
