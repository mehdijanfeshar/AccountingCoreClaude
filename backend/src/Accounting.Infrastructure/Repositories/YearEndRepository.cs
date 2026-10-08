using Accounting.Application.Vouchers.YearEnd;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>
/// خواندن مانده‌ها و پیکربندی سند افتتاحیه/اختتامیه، و stage سندهای ساخته‌شده. مستقیم از جدول‌ها
/// (نه از VW_REPORTFOROPENINGVOUCHER / VW_REPORTFORCLOSINGVOUCHER): View افتتاحیه سال و واحد و
/// حذف‌شده‌ها را فیلتر نمی‌کند و View اختتامیه از جدول‌های موقت (tmp) می‌خواند.
///
/// Oracle: no AnyAsync, no boolean projections, no Contains over a long id list (ORA-01795);
/// joins and small literal IN lists only.
/// </summary>
public sealed class YearEndRepository(LegacyDbContext db) : IYearEndRepository
{
    public async Task<Guid?> GetVahedTypeIdAsync(string vahedCode, CancellationToken ct)
        => await db.TB_VAHED_INFOs.AsNoTracking()
            .Where(v => v.VAHEDCODE == vahedCode)
            .Select(v => (Guid?)v.VAHEDTYPE_ID)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<YearEndBalance>> GetBalancesAsync(
        string vahedCode, string year, IReadOnlyCollection<char> groups, bool includeClosing, CancellationToken ct)
    {
        var groupList = groups.Select(g => g.ToString()).ToList();

        var heads = db.TB_VOUCHERSHEADs.AsNoTracking()
            .Where(h => h.ISDELETED != true && h.VAHEDCODE == vahedCode && h.YEAR == year);
        // Decided here, not inside the SQL: a C# bool in the predicate would reach Oracle as a boolean.
        if (!includeClosing)
            heads = heads.Where(h => h.FLAG_STATE == null || h.FLAG_STATE != YearEndKinds.ClosingFlag);

        var lines =
            from d in db.TB_VOUCHERSDETAILs.AsNoTracking()
            join h in heads on d.VOUCHERSHEAD_ID equals h.ID
            join a in db.TB_ACCOUNTCODEs.AsNoTracking() on d.ACCOUNT_ID equals a.ID
            where d.ISDELETED != true && a.ACCCODE != null && groupList.Contains(a.ACCCODE.Substring(0, 1))
            select new { DetailId = d.ID, AccountId = a.ID, a.ACCCODE, a.ACCCODENAME, d.DEBTOR, d.CREDITOR };

        var rows = await lines.ToListAsync(ct);

        var links = await (
            from l in db.TB_VOUCHERDETAIL_LINK_TAFSILIs.AsNoTracking()
            join x in lines on l.VOUCHERSDETAIL_ID equals x.DetailId
            join lv in db.TB_LEVEL_TAFSILs.AsNoTracking() on l.LEVEL_ID equals lv.ID into lvj
            from lv in lvj.DefaultIfEmpty()
            join t in db.TB_TAFSILIs.AsNoTracking() on l.TAFSILI_ID equals t.ID into tj
            from t in tj.DefaultIfEmpty()
            where !l.ISDELETED
            select new { l.VOUCHERSDETAIL_ID, l.LEVEL_ID, LevelCode = lv.LEVEL_CODE, l.TAFSILI_ID, t.TAFSILI_CODE, t.TAFSILI_NAME })
            .ToListAsync(ct);

        var linksByDetail = links
            .GroupBy(l => l.VOUCHERSDETAIL_ID)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<YearEndTafsili>)g
                    .GroupBy(l => l.LEVEL_ID)           // one تفصیلی per level; a duplicate link row counts once
                    .Select(x => x.First())
                    .OrderBy(l => l.LevelCode, StringComparer.Ordinal)
                    .Select(l => new YearEndTafsili(l.LEVEL_ID, l.LevelCode, l.TAFSILI_ID, l.TAFSILI_CODE, l.TAFSILI_NAME))
                    .ToList());

        return rows
            .Select(r => new
            {
                r.AccountId, r.ACCCODE, r.ACCCODENAME,
                Tafsilis = linksByDetail.GetValueOrDefault(r.DetailId) ?? [],
                Net = (r.DEBTOR ?? 0) - (r.CREDITOR ?? 0),
            })
            .GroupBy(r => (r.AccountId, Key: string.Join("|", r.Tafsilis.Select(t => $"{t.LevelId}:{t.TafsiliId}"))))
            .Select(g => new YearEndBalance(
                g.Key.AccountId, g.First().ACCCODE!, g.First().ACCCODENAME, g.First().Tafsilis, g.Sum(x => x.Net)))
            .ToList();
    }

    public async Task<IReadOnlySet<Guid>> GetExceptionAccountIdsAsync(Guid vahedTypeId, CancellationToken ct)
        => (await db.TB_ACCOUNTEXCEPTIONs.AsNoTracking()
                .Where(e => !e.ISDELETED && e.VAHEDTYPE_ID == vahedTypeId)
                .Select(e => e.ACCOUNTCOE_ID)
                .ToListAsync(ct))
            .ToHashSet();

    public async Task<YearEndAccountRef?> GetInterfaceAccountAsync(InterfaceType type, CancellationToken ct)
        => await (
            from i in db.TB_ACCOUNTCODE_INTERFACEs.AsNoTracking()
            join a in db.TB_ACCOUNTCODEs.AsNoTracking() on i.ACCOUNTCODEID equals a.ID
            where !i.ISDELETED && i.TYPE == type && a.ISDELETED != true
            orderby i.CREATEDDATE descending
            select new YearEndAccountRef(a.ID, a.ACCCODE!, a.ACCCODENAME))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyDictionary<Guid, YearEndAccountRef>> GetClosingRabetMapAsync(Guid vahedTypeId, string year, CancellationToken ct)
    {
        var configured = await (
            from rc in db.TB_RABET_CLOSINGs.AsNoTracking()
            join a in db.TB_ACCOUNTCODEs.AsNoTracking() on rc.ACCOUNTCODE_ID equals a.ID
            join r in db.TB_ACCOUNTCODEs.AsNoTracking() on rc.ACCOUNTCODE_RABET_ID equals r.ID
            where rc.ISDELETED != true && rc.YEAR == year && rc.VAHEDTYPE_ID == vahedTypeId
                  && a.ACCCODE != null && r.ACCCODE != null
            select new { a.ID, a.ACCCODE, Rabet = new YearEndAccountRef(r.ID, r.ACCCODE!, r.ACCCODENAME) })
            .ToListAsync(ct);

        var map = new Dictionary<Guid, YearEndAccountRef>();
        var kols = configured.Where(c => c.ACCCODE!.Length == 4).ToList();
        if (kols.Count > 0)
        {
            // A rabet set on a کل reaches every معین under it (VW_RABETCLOSING_ACCOUNTS).
            var moeins = await db.TB_ACCOUNTCODEs.AsNoTracking()
                .Where(a => a.ISDELETED != true && a.ACCCODE != null && a.ACCCODE.Length == 6)
                .Select(a => new { a.ID, a.ACCCODE })
                .ToListAsync(ct);
            foreach (var kol in kols)
                foreach (var m in moeins.Where(m => m.ACCCODE!.StartsWith(kol.ACCCODE!, StringComparison.Ordinal)))
                    map[m.ID] = kol.Rabet;
        }
        // A rabet set on the معین itself wins over its کل.
        foreach (var moein in configured.Where(c => c.ACCCODE!.Length == 6))
            map[moein.ID] = moein.Rabet;
        return map;
    }

    public async Task<IReadOnlyList<string>> GetDocNumsWithAccountAsync(string vahedCode, string year, Guid accountId, CancellationToken ct)
        => await (
            from h in db.TB_VOUCHERSHEADs.AsNoTracking()
            join d in db.TB_VOUCHERSDETAILs.AsNoTracking() on h.ID equals d.VOUCHERSHEAD_ID
            where h.ISDELETED != true && d.ISDELETED != true && h.VAHEDCODE == vahedCode && h.YEAR == year
                  && d.ACCOUNT_ID == accountId
            select h.DOC_NUM ?? "")
            .Distinct()
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetClosingDocNumsAsync(string vahedCode, string year, CancellationToken ct)
        => await db.TB_VOUCHERSHEADs.AsNoTracking()
            .Where(h => h.ISDELETED != true && h.VAHEDCODE == vahedCode && h.YEAR == year && h.FLAG_STATE == YearEndKinds.ClosingFlag)
            .Select(h => h.DOC_NUM ?? "")
            .ToListAsync(ct);

    public Task<int> CountDraftVouchersAsync(string vahedCode, string year, CancellationToken ct)
        => db.TB_VOUCHERSHEADs.AsNoTracking()
            .CountAsync(h => h.ISDELETED != true && h.VAHEDCODE == vahedCode && h.YEAR == year && h.DOCLIFE == DocLife.Draft, ct);

    public async Task StageAsync(
        TB_VOUCHERSHEAD head, IReadOnlyList<TB_VOUCHERSDETAIL> details, IReadOnlyList<TB_VOUCHERDETAIL_LINK_TAFSILI> links, CancellationToken ct)
    {
        await db.TB_VOUCHERSHEADs.AddAsync(head, ct);
        await db.TB_VOUCHERSDETAILs.AddRangeAsync(details, ct);
        await db.TB_VOUCHERDETAIL_LINK_TAFSILIs.AddRangeAsync(links, ct);
    }
}
