using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IVoucherHeadRepository"/>. Only stages the
/// entity via <see cref="Microsoft.EntityFrameworkCore.DbSet{TEntity}.AddAsync"/> — never
/// calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class VoucherHeadRepository : IVoucherHeadRepository
{
    private readonly LegacyDbContext _dbContext;

    public VoucherHeadRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_VOUCHERSHEAD voucherHead, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_VOUCHERSHEADs.AddAsync(voucherHead, cancellationToken);
    }

    public async Task<TB_VOUCHERSHEAD?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_VOUCHERSHEADs
            .FirstOrDefaultAsync(h => h.ID == id, cancellationToken);
    }

    public async Task<int> SoftDeleteDetailTreeAsync(
        Guid headId,
        string? changeUserId,
        DateTime updatedDate,
        CancellationToken cancellationToken = default)
    {
        // Single query loads EVERY detail row of the head (not filtered by ISDELETED in SQL),
        // because the level-3 (tafsili link) scope below deliberately needs the full detail-ID
        // set of the head, not just the ones freshly soft-deleted here — see the scoping
        // rationale on IVoucherHeadRepository.SoftDeleteDetailTreeAsync. Partitioning into
        // "needs soft-delete" vs. "full ID set" happens in memory after this one query.
        var allDetailLines = await _dbContext.TB_VOUCHERSDETAILs
            .Where(d => d.VOUCHERSHEAD_ID == headId)
            .ToListAsync(cancellationToken);

        // Load-and-mutate (not ExecuteUpdateAsync) so the change is staged on the change
        // tracker and persisted by the handler's single SaveChangesAsync. The predicate is
        // written as `== null || == false` (not `!= true`) so that NULL ISDELETED rows are
        // included, matching how the head handler already treats null as "not deleted".
        var detailLinesToSoftDelete = allDetailLines
            .Where(d => d.ISDELETED == null || d.ISDELETED == false)
            .ToList();

        foreach (var detailLine in detailLinesToSoftDelete)
        {
            detailLine.ISDELETED = true;
            detailLine.CHANGEUSERID = changeUserId;
            detailLine.UPDATEDDATE = updatedDate;
        }

        // Level 3: scoped to ALL detail rows of the head (allDetailLines), not just
        // detailLinesToSoftDelete — see the "dangling link" rationale on the interface. Bounded
        // to tens of IDs per voucher, so an EF `Contains`-translated SQL `IN (...)` is fine here.
        var allDetailIds = allDetailLines.Select(d => d.ID).ToList();

        var linkRowsToSoftDelete = await _dbContext.TB_VOUCHERDETAIL_LINK_TAFSILIs
            .Where(l => allDetailIds.Contains(l.VOUCHERSDETAIL_ID) && l.ISDELETED == false)
            .ToListAsync(cancellationToken);

        foreach (var linkRow in linkRowsToSoftDelete)
        {
            linkRow.ISDELETED = true;
            linkRow.CHANGEUSERID = changeUserId;
            linkRow.UPDATEDDATE = updatedDate;
        }

        return detailLinesToSoftDelete.Count + linkRowsToSoftDelete.Count;
    }
}
