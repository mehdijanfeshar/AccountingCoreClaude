using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class TreasurySettingRepository : ITreasurySettingRepository
{
    private readonly LegacyDbContext _dbContext;

    public TreasurySettingRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_TR_SETTING setting, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_TR_SETTINGs.AddAsync(setting, cancellationToken);
    }

    public Task<TB_TR_SETTING?> GetForUpdateAsync(string vahedCode, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_TR_SETTINGs
            .FirstOrDefaultAsync(s => s.VAHEDCODE == vahedCode && !s.ISDELETED, cancellationToken);
    }
}
