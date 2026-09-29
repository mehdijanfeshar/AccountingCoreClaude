using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Queries;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class TreasurySettingReadRepository : ITreasurySettingReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public TreasurySettingReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TreasurySettingDto?> GetByVahedAsync(string vahedCode, CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_TR_SETTINGs
            .AsNoTracking()
            .Where(s => s.VAHEDCODE == vahedCode && !s.ISDELETED)
            .Select(s => new TreasurySettingDto(s.ID, s.CEO_APPROVAL_THRESHOLD, s.BULK_APPROVE_LIMIT))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
