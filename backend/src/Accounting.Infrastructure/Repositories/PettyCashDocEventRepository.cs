using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;

namespace Accounting.Infrastructure.Repositories;

public sealed class PettyCashDocEventRepository : IPettyCashDocEventRepository
{
    private readonly LegacyDbContext _dbContext;

    public PettyCashDocEventRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PC_DOC_EVENT docEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PC_DOC_EVENTs.AddAsync(docEvent, cancellationToken);
    }
}
