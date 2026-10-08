using Accounting.Application.RabetClosings;
using Accounting.Application.Vouchers.YearEnd;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

/// <summary>رابط اختتامیه. Id lists here are a handful of user selections, far below Oracle's IN limit.</summary>
public sealed class RabetClosingRepository(LegacyDbContext db) : IRabetClosingRepository
{
    public async Task<IReadOnlyList<RabetClosingDto>> ListAsync(string? year, CancellationToken ct)
    {
        var rows = db.TB_RABET_CLOSINGs.AsNoTracking().Where(r => r.ISDELETED != true);
        if (!string.IsNullOrWhiteSpace(year))
            rows = rows.Where(r => r.YEAR == year);

        return await (
            from r in rows
            join t in db.TB_VAHED_TYPEs.AsNoTracking() on r.VAHEDTYPE_ID equals t.ID into tj
            from t in tj.DefaultIfEmpty()
            join a in db.TB_ACCOUNTCODEs.AsNoTracking() on r.ACCOUNTCODE_ID equals a.ID into aj
            from a in aj.DefaultIfEmpty()
            join b in db.TB_ACCOUNTCODEs.AsNoTracking() on r.ACCOUNTCODE_RABET_ID equals b.ID into bj
            from b in bj.DefaultIfEmpty()
            orderby r.YEAR descending, t.TYPENAME, a.ACCCODE
            select new RabetClosingDto(r.ID, r.YEAR, r.VAHEDTYPE_ID, t.TYPENAME, r.TYPEACCOUNTCODE,
                r.ACCOUNTCODE_ID, a.ACCCODE, a.ACCCODENAME, r.ACCOUNTCODE_RABET_ID, b.ACCCODE, b.ACCCODENAME, r.TITLE))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RabetClosingAccount>> GetAccountsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
        => await db.TB_ACCOUNTCODEs.AsNoTracking()
            .Where(a => ids.Contains(a.ID) && a.ISDELETED != true)
            .Select(a => new RabetClosingAccount(a.ID, a.ACCCODE, a.ACCCODENAME))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Guid>> GetExistingVahedTypeIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct)
        => await db.TB_VAHED_TYPEs.AsNoTracking().Where(t => ids.Contains(t.ID)).Select(t => t.ID).ToListAsync(ct);

    public async Task<IReadOnlyList<(Guid VahedTypeId, Guid AccountId)>> GetExistingPairsAsync(string year, CancellationToken ct)
        => (await db.TB_RABET_CLOSINGs.AsNoTracking()
                .Where(r => r.ISDELETED != true && r.YEAR == year && r.ACCOUNTCODE_ID != null)
                .Select(r => new { r.VAHEDTYPE_ID, r.ACCOUNTCODE_ID })
                .ToListAsync(ct))
            .Select(r => (r.VAHEDTYPE_ID, r.ACCOUNTCODE_ID!.Value))
            .ToList();

    public Task<TB_RABET_CLOSING?> GetForUpdateAsync(Guid id, CancellationToken ct)
        => db.TB_RABET_CLOSINGs.FirstOrDefaultAsync(r => r.ID == id, ct);

    public Task<int> CountClosingVouchersAsync(string year, Guid vahedTypeId, CancellationToken ct)
        => (from h in db.TB_VOUCHERSHEADs.AsNoTracking()
            join v in db.TB_VAHED_INFOs.AsNoTracking() on h.VAHEDCODE equals v.VAHEDCODE
            where h.ISDELETED != true && h.YEAR == year && h.FLAG_STATE == YearEndKinds.ClosingFlag && v.VAHEDTYPE_ID == vahedTypeId
            select h.ID).CountAsync(ct);

    public Task AddRangeAsync(IEnumerable<TB_RABET_CLOSING> rows, CancellationToken ct) => db.TB_RABET_CLOSINGs.AddRangeAsync(rows, ct);
}
