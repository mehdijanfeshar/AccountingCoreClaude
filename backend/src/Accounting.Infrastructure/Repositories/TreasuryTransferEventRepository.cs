using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;

namespace Accounting.Infrastructure.Repositories;

public sealed class TreasuryTransferEventRepository : ITreasuryTransferEventRepository
{
    private readonly LegacyDbContext _dbContext;

    public TreasuryTransferEventRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_TR_TRANSFER_EVENT transferEvent, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TR_TRANSFER_EVENTs.AddAsync(transferEvent, cancellationToken);
    }
}
