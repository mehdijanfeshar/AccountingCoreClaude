using Accounting.Application.Common.Interfaces;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class AgePyramidReadRepository(LegacyDbContext db) : IAgePyramidReadRepository
{
    public async Task<IReadOnlyList<AgePyramidRawRow>> GetAllYearsAsync(
        string vahedCode, IReadOnlyCollection<Guid>? accountIds, CancellationToken cancellationToken = default)
    {
        // آخرین FLAG تعریف‌شده برای هر معینِ شناسه‌دار این واحد (در هر سالی که تعریف شده) — تعریف یک‌بار
        // در ابتدا جدا خوانده می‌شود تا با Join مستقیم به TB_VOUCHERSDETAIL، ردیف‌های هر سالِ تعریف چندبار
        // تکرار (fan-out) نشوند.
        var definitions = await db.TB_ATTRIBFORACCOUNTCODEs.AsNoTracking()
            .Where(a => a.ISDELETED != true && a.VAHEDCODE == vahedCode)
            .Select(a => new { a.ACCOUNTCODE_ID, a.FLAG, a.YEAR })
            .ToListAsync(cancellationToken);

        var flagByAccount = definitions
            .GroupBy(d => d.ACCOUNTCODE_ID)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.YEAR, StringComparer.Ordinal).First().FLAG);

        var wantedAccountIds = (IReadOnlyCollection<Guid>)(accountIds is { Count: > 0 }
            ? flagByAccount.Keys.Where(accountIds.Contains).ToList()
            : flagByAccount.Keys.ToList());
        if (wantedAccountIds.Count == 0)
            return [];

        var raw = await (
                from d in db.TB_VOUCHERSDETAILs.AsNoTracking()
                where d.ISDELETED != true && d.VOUCHERSHEAD!.ISDELETED != true && d.VOUCHERSHEAD.VAHEDCODE == vahedCode
                      && d.ACCOUNT_ID != null && wantedAccountIds.Contains(d.ACCOUNT_ID.Value)
                join a in db.TB_ACCOUNTCODEs.AsNoTracking() on d.ACCOUNT_ID equals (Guid?)a.ID
                select new
                {
                    AccountId = a.ID,
                    a.ACCCODE,
                    a.ACCCODENAME,
                    Identifier = d.TB_ATTRIBSINVOUCHERs.Where(t => t.ISDELETED != true).Select(t => t.ATTRIBUTEVALUE).FirstOrDefault(),
                    Year = d.VOUCHERSHEAD!.YEAR,
                    d.DEBTOR,
                    d.CREDITOR,
                })
            .ToListAsync(cancellationToken);

        return raw
            .Where(r => r.Identifier != null && r.Year != null)
            .GroupBy(r => (r.AccountId, r.ACCCODE, r.ACCCODENAME, r.Identifier, r.Year))
            .Select(g => new AgePyramidRawRow(
                g.Key.AccountId, g.Key.ACCCODE!, g.Key.ACCCODENAME, flagByAccount.GetValueOrDefault(g.Key.AccountId),
                g.Key.Identifier!, g.Key.Year!, g.Sum(x => x.DEBTOR ?? 0), g.Sum(x => x.CREDITOR ?? 0)))
            .ToList();
    }
}
