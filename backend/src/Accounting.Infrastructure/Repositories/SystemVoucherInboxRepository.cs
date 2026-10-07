using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class SystemVoucherInboxRepository : ISystemVoucherInboxRepository
{
    private readonly LegacyDbContext _db;

    public SystemVoucherInboxRepository(LegacyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SystemVoucherInboxRow>> ListAsync(string vahedCode, string? year, bool includeReceived, CancellationToken ct)
    {
        var heads = _db.TB_TMP_VOUCHERHEADs.AsNoTracking()
            .Where(h => h.VAHEDCODE == vahedCode && h.ISDELETED != true);
        if (!string.IsNullOrWhiteSpace(year))
            heads = heads.Where(h => h.YEAR == year);
        if (!includeReceived)
            heads = heads.Where(h => h.VOUCHERSHEAD_ID == null);

        var list = await heads
            .OrderByDescending(h => h.CREATEDDATE)
            .Take(500)
            .Select(h => new
            {
                h.ID, h.SYS_TYPE, h.DATE_DOC, h.YEAR, h.HEAD_DESC, h.CREATEDDATE, h.VOUCHERSHEAD_ID,
                DocNum = h.VOUCHERSHEAD != null ? h.VOUCHERSHEAD.DOC_NUM : null,
            })
            .ToListAsync(ct);

        var ids = list.Select(h => h.ID).ToList();
        var totals = new Dictionary<Guid, (int Count, decimal Debtor, decimal Creditor)>();
        foreach (var chunk in ids.Chunk(500))
        {
            var part = await _db.TB_TMP_VOUCHERSDETAILs.AsNoTracking()
                .Where(d => d.TMPVOUCHERHEAD_ID != null && chunk.Contains(d.TMPVOUCHERHEAD_ID.Value) && d.ISDELETED != true)
                .GroupBy(d => d.TMPVOUCHERHEAD_ID!.Value)
                .Select(g => new { g.Key, Count = g.Count(), Debtor = g.Sum(d => d.DEBTOR ?? 0), Creditor = g.Sum(d => d.CREDITOR ?? 0) })
                .ToListAsync(ct);
            foreach (var p in part)
                totals[p.Key] = (p.Count, p.Debtor, p.Creditor);
        }

        return list.Select(h =>
        {
            var t = totals.TryGetValue(h.ID, out var found) ? found : (0, 0m, 0m);
            return new SystemVoucherInboxRow(h.ID, h.SYS_TYPE, h.DATE_DOC, h.YEAR, h.HEAD_DESC, h.CREATEDDATE,
                t.Item1, t.Item2, t.Item3, h.VOUCHERSHEAD_ID, h.DocNum);
        }).ToList();
    }

    public async Task<(TB_TMP_VOUCHERHEAD Head, IReadOnlyList<TB_TMP_VOUCHERSDETAIL> Lines)?> GetForReceiveAsync(
        Guid id, string vahedCode, CancellationToken ct)
    {
        var head = await _db.TB_TMP_VOUCHERHEADs
            .FirstOrDefaultAsync(h => h.ID == id && h.VAHEDCODE == vahedCode && h.ISDELETED != true, ct);
        if (head is null)
            return null;

        var lines = await _db.TB_TMP_VOUCHERSDETAILs.AsNoTracking()
            .Where(d => d.TMPVOUCHERHEAD_ID == id && d.ISDELETED != true)
            .OrderBy(d => d.RADIF)
            .ToListAsync(ct);
        return (head, lines);
    }

    public async Task<IReadOnlyDictionary<string, (Guid Id, string Name)>> ResolveMoinsAsync(IReadOnlyCollection<string> codes, CancellationToken ct)
    {
        var list = codes.Distinct().ToList();
        if (list.Count == 0)
            return new Dictionary<string, (Guid, string)>();

        var rows = await _db.TB_ACCOUNTCODEs.AsNoTracking()
            .Where(a => a.ACCCODE != null && list.Contains(a.ACCCODE) && a.ISDELETED != true && a.TYPECODE == TypeCodes.Moin)
            .Select(a => new { a.ID, a.ACCCODE, a.ACCCODENAME })
            .ToListAsync(ct);
        return rows
            .GroupBy(r => r.ACCCODE!)
            .ToDictionary(g => g.Key, g => (g.First().ID, g.First().ACCCODENAME ?? string.Empty));
    }

    public async Task<IReadOnlyDictionary<string, (Guid Id, string Name)>> ResolveTafsilisAsync(
        IReadOnlyCollection<string> codes, string vahedCode, CancellationToken ct)
    {
        var list = codes.Distinct().ToList();
        if (list.Count == 0)
            return new Dictionary<string, (Guid, string)>();

        var rows = await _db.TB_TAFSILIs.AsNoTracking()
            .Where(t => t.TAFSILI_CODE != null && list.Contains(t.TAFSILI_CODE) && t.ISDELETED != true
                        && (t.VAHEDCODE == null || t.VAHEDCODE == vahedCode || t.OWNER == Owners.Global))
            .Select(t => new { t.ID, t.TAFSILI_CODE, t.TAFSILI_NAME, Own = t.VAHEDCODE == vahedCode })
            .ToListAsync(ct);
        return rows
            .GroupBy(r => r.TAFSILI_CODE!)
            .ToDictionary(g => g.Key, g =>
            {
                var pick = g.OrderByDescending(r => r.Own).First();
                return (pick.ID, pick.TAFSILI_NAME ?? string.Empty);
            });
    }

    public async Task<IReadOnlyDictionary<int, Guid>> GetLevelIdsByCodeAsync(CancellationToken ct)
    {
        var rows = await _db.TB_LEVEL_TAFSILs.AsNoTracking()
            .Where(l => !l.ISDELETED)
            .Select(l => new { l.ID, l.LEVEL_CODE })
            .ToListAsync(ct);
        var map = new Dictionary<int, Guid>();
        foreach (var r in rows)
        {
            if (int.TryParse(r.LEVEL_CODE, out var n) && !map.ContainsKey(n))
                map[n] = r.ID;
        }

        return map;
    }
}
