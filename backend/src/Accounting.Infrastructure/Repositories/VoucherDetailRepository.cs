using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// EF Core (Oracle) implementation of <see cref="IVoucherDetailRepository"/>. Only stages
/// changes — never calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="Application.Common.Interfaces.IUnitOfWork"/>.
/// </summary>
public sealed class VoucherDetailRepository : IVoucherDetailRepository
{
    private readonly LegacyDbContext _dbContext;

    public VoucherDetailRepository(LegacyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(TB_VOUCHERSDETAIL voucherDetail, CancellationToken cancellationToken = default)
    {
        await _dbContext.TB_VOUCHERSDETAILs.AddAsync(voucherDetail, cancellationToken);
    }

    public async Task<TB_VOUCHERSDETAIL?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TB_VOUCHERSDETAILs
            .FirstOrDefaultAsync(d => d.ID == id, cancellationToken);
    }

    public async Task<int> SoftDeleteTafsiliLinksAsync(
        Guid detailId,
        string? changeUserId,
        DateTime updatedDate,
        CancellationToken cancellationToken = default)
    {
        // TB_VOUCHERDETAIL_LINK_TAFSILI.ISDELETED is a non-nullable bool (verified against the
        // Fluent mapping), so the filter is a plain `== false` — no NULL branch, unlike the
        // two-level head cascade in VoucherHeadRepository.SoftDeleteDetailTreeAsync.
        var linkRowsToSoftDelete = await _dbContext.TB_VOUCHERDETAIL_LINK_TAFSILIs
            .Where(l => l.VOUCHERSDETAIL_ID == detailId && l.ISDELETED == false)
            .ToListAsync(cancellationToken);

        // Load-and-mutate (not ExecuteUpdateAsync) so the change is staged on the change
        // tracker and persisted by the handler's single SaveChangesAsync — same rationale as
        // VoucherHeadRepository.SoftDeleteDetailTreeAsync.
        foreach (var linkRow in linkRowsToSoftDelete)
        {
            linkRow.ISDELETED = true;
            linkRow.CHANGEUSERID = changeUserId;
            linkRow.UPDATEDDATE = updatedDate;
        }

        return linkRowsToSoftDelete.Count;
    }
}
