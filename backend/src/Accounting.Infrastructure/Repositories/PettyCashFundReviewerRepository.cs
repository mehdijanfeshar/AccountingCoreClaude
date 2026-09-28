using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
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

    public Task<TB_PC_REVIEWER?> GetByFundUserAndRoleAsync(
        Guid fundId,
        string reviewerUserId,
        PettyCashRole role,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TB_PC_REVIEWERs
            .FirstOrDefaultAsync(
                r => r.FUND_ID == fundId && r.REVIEWER_USERID == reviewerUserId && r.ROLE == role,
                cancellationToken);
    }

    public async Task<IReadOnlyList<PettyCashRole>> GetActiveRolesAsync(
        Guid fundId,
        string reviewerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_PC_REVIEWERs
            .AsNoTracking()
            .Where(r => r.FUND_ID == fundId && r.REVIEWER_USERID == reviewerUserId && !r.ISDELETED)
            .Select(r => r.ROLE)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<TB_PC_REVIEWER?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.TB_PC_REVIEWERs
            .FirstOrDefaultAsync(r => r.ID == id, cancellationToken);

        VahedOwnership.EnsureOwned(entity?.VAHEDCODE, vahedCode, id, "PettyCashReviewer");

        return entity;
    }
}
