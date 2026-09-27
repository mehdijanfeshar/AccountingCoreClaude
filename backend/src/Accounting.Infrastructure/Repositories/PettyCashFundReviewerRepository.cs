using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class PettyCashFundReviewerRepository : IPettyCashFundReviewerRepository
{
    private readonly LegacyDbContext _dbContext;

    public PettyCashFundReviewerRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_PC_REVIEWER reviewer, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_PC_REVIEWERs.AddAsync(reviewer, cancellationToken);
    }

    public Task<TB_PC_REVIEWER?> GetByFundAndUserIdAsync(
        Guid revolvingFundId,
        string reviewerUserId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_PC_REVIEWERs
            .FirstOrDefaultAsync(
                r => r.REVOLVINGFUND_ID == revolvingFundId && r.REVIEWER_USERID == reviewerUserId,
                cancellationToken);
    }

    public async Task<TB_PC_REVIEWER?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_PC_REVIEWERs
            .FirstOrDefaultAsync(r => r.ID == id, cancellationToken);

        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "PettyCashReviewer");

        return entity;
    }
}
