using Accounting.Application.Common.Interfaces;
using Accounting.Application.Treasury.Queries;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class TreasuryRoleReadRepository : ITreasuryRoleReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public TreasuryRoleReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<TreasuryRoleDto>> GetByVahedAsync(string vahedCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_TR_ROLEs
            .AsNoTracking()
            .Where(r => r.VAHEDCODE == vahedCode && !r.ISDELETED)
            .Select(r => new TreasuryRoleDto(r.ID, r.USERID, r.USER_NAME, r.ROLE))
            .ToListAsync(cancellationToken);
    }
}
