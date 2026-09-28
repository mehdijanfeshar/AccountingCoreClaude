using Accounting.Application.Common.Interfaces;
using Accounting.Application.PettyCash.Queries;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class PettyCashFundReviewerReadRepository : IPettyCashFundReviewerReadRepository
{
    private readonly LegacyDbContext _dbContext;

    public PettyCashFundReviewerReadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PettyCashFundReviewerDto>> GetByFundIdAsync(
        Guid fundId,
        string vahedCode,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_PC_REVIEWERs
            .AsNoTracking()
            .Where(r => r.FUND_ID == fundId && !r.ISDELETED && r.VAHEDCODE == vahedCode)
            .Select(r => new PettyCashFundReviewerDto(r.ID, r.FUND_ID, r.REVIEWER_USERID, r.REVIEWER_NAME, r.ROLE))
            .ToListAsync(cancellationToken);
    }
}
