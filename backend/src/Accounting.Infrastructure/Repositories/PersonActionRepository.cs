using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IPersonActionRepository"/>. Only stages the
/// entity — never calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class PersonActionRepository : IPersonActionRepository
{
    private readonly LegacyDbContext _dbContext;

    public PersonActionRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PERSON_ACTION personAction, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PERSON_ACTIONs.AddAsync(personAction, cancellationToken);
    }

    public async Task<TB_PERSON_ACTION?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_PERSON_ACTIONs
            .FirstOrDefaultAsync(p => p.ID == id, cancellationToken);
    }
}
