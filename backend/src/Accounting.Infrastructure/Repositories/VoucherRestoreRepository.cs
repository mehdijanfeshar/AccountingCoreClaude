using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class VoucherRestoreRepository : IVoucherRestoreRepository
{
    private readonly LegacyDbContext _db;

    public VoucherRestoreRepository(LegacyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DeletedVoucherRow>> ListDeletedAsync(string vahedCode, string year, CancellationToken ct)
    {
        var heads = await _db.TB_VOUCHERSHEADs.AsNoTracking()
            .Where(h => h.VAHEDCODE == vahedCode && h.YEAR == year && h.ISDELETED == true)
            .OrderByDescending(h => h.UPDATEDDATE)
            .Take(300)
            .Select(h => new { h.ID, h.DOC_NUM, h.DATE_DOC, h.HEAD_DESC, h.UPDATEDDATE, h.CHANGEUSERID })
            .ToListAsync(ct);

        var ids = heads.Select(h => h.ID).ToList();
        var lines = await _db.TB_VOUCHERSDETAILs.AsNoTracking()
            .Where(d => d.VOUCHERSHEAD_ID != null && ids.Contains(d.VOUCHERSHEAD_ID.Value))
            .Select(d => new { HeadId = d.VOUCHERSHEAD_ID!.Value, d.UPDATEDDATE, d.ISDELETED, d.DEBTOR, d.CREDITOR })
            .ToListAsync(ct);

        return heads.Select(h =>
        {
            // فقط ردیف‌هایی که با خود سرسند حذف شدند — همان‌ها که بازگردانده می‌شوند.
            var mine = lines.Where(l => l.HeadId == h.ID && l.ISDELETED == true && l.UPDATEDDATE == h.UPDATEDDATE).ToList();
            return new DeletedVoucherRow(h.ID, h.DOC_NUM, h.DATE_DOC, h.HEAD_DESC, h.UPDATEDDATE, h.CHANGEUSERID,
                mine.Count, mine.Sum(l => l.DEBTOR ?? 0), mine.Sum(l => l.CREDITOR ?? 0));
        }).ToList();
    }

    public Task<TB_VOUCHERSHEAD?> GetDeletedForRestoreAsync(Guid id, string vahedCode, CancellationToken ct)
        => _db.TB_VOUCHERSHEADs.FirstOrDefaultAsync(h => h.ID == id && h.VAHEDCODE == vahedCode && h.ISDELETED == true, ct);

    public async Task<bool> IsDocNumTakenAsync(string vahedCode, string year, string docNum, Guid excludeId, CancellationToken ct)
        => await _db.TB_VOUCHERSHEADs.CountAsync(
            h => h.VAHEDCODE == vahedCode && h.YEAR == year && h.DOC_NUM == docNum && h.ID != excludeId && h.ISDELETED != true, ct) > 0;

    public async Task<int> RestoreTreeAsync(Guid headId, DateTime? deletedAt, string? userId, DateTime now, CancellationToken ct)
    {
        var details = await _db.TB_VOUCHERSDETAILs
            .Where(d => d.VOUCHERSHEAD_ID == headId && d.ISDELETED == true && d.UPDATEDDATE == deletedAt)
            .ToListAsync(ct);
        foreach (var d in details)
        {
            d.ISDELETED = false;
            d.CHANGEUSERID = userId;
            d.UPDATEDDATE = now;
        }

        var detailIds = details.Select(d => d.ID).ToList();
        var links = await _db.TB_VOUCHERDETAIL_LINK_TAFSILIs
            .Where(l => detailIds.Contains(l.VOUCHERSDETAIL_ID) && l.ISDELETED && l.UPDATEDDATE == deletedAt)
            .ToListAsync(ct);
        foreach (var l in links)
        {
            l.ISDELETED = false;
            l.CHANGEUSERID = userId;
            l.UPDATEDDATE = now;
        }

        return details.Count;
    }
}
