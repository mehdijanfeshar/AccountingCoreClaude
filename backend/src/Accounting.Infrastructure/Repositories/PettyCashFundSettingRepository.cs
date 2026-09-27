using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class PettyCashFundSettingRepository : IPettyCashFundSettingRepository
{
    private readonly LegacyDbContext _dbContext;

    public PettyCashFundSettingRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PC_FUND_SETTING setting, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PC_FUND_SETTINGs.AddAsync(setting, cancellationToken);
    }

    public Task<TB_PC_FUND_SETTING?> GetByFundIdAsync(Guid revolvingFundId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_PC_FUND_SETTINGs
            .FirstOrDefaultAsync(s => s.REVOLVINGFUND_ID == revolvingFundId, cancellationToken);
    }
}
