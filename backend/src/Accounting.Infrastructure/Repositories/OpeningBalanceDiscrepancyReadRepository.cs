using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.OpeningBalanceDiscrepancy;
using Accounting.Domain.Entity;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class OpeningBalanceDiscrepancyReadRepository(LegacyDbContext db) : IOpeningBalanceDiscrepancyReadRepository
{
    public async Task<IReadOnlyList<OpeningBalanceAccountLine>> GetOpeningLinesAsync(
        string vahedCode, string year, Guid openingInterfaceAccountId, CancellationToken cancellationToken = default)
    {
        var headIds = await db.TB_VOUCHERSHEADs.AsNoTracking()
            .Where(h => h.ISDELETED != true && h.VAHEDCODE == vahedCode && h.YEAR == year && h.ISAUTOMATIC == true
                        && h.TB_VOUCHERSDETAILs.Any(d => d.ISDELETED != true && d.ACCOUNT_ID == openingInterfaceAccountId))
            .Select(h => h.ID)
            .ToListAsync(cancellationToken);
        if (headIds.Count == 0)
            return [];

        var details = db.TB_VOUCHERSDETAILs.AsNoTracking()
            .Where(d => d.ISDELETED != true && d.VOUCHERSHEAD_ID != null && headIds.Contains(d.VOUCHERSHEAD_ID!.Value)
                        && d.ACCOUNT_ID != openingInterfaceAccountId);
        return await BalancesByAttributeAsync(details, cancellationToken);
    }

    public Task<IReadOnlyList<OpeningBalanceAccountLine>> GetYearBalancesByAttributeAsync(
        string vahedCode, string year, CancellationToken cancellationToken = default)
    {
        var details = db.TB_VOUCHERSDETAILs.AsNoTracking()
            .Where(d => d.ISDELETED != true && d.VOUCHERSHEAD!.ISDELETED != true
                        && d.VOUCHERSHEAD.VAHEDCODE == vahedCode && d.VOUCHERSHEAD.YEAR == year);
        return BalancesByAttributeAsync(details, cancellationToken);
    }

    private async Task<IReadOnlyList<OpeningBalanceAccountLine>> BalancesByAttributeAsync(
        IQueryable<TB_VOUCHERSDETAIL> details, CancellationToken cancellationToken)
    {
        var rows = await (
                from d in details
                join a in db.TB_ACCOUNTCODEs.AsNoTracking() on d.ACCOUNT_ID equals (Guid?)a.ID
                where a.ACCCODE != null
                select new
                {
                    AccountId = a.ID,
                    a.ACCCODE,
                    a.ACCCODENAME,
                    Value = d.TB_ATTRIBSINVOUCHERs.Where(t => t.ISDELETED != true).Select(t => t.ATTRIBUTEVALUE).FirstOrDefault(),
                    d.DEBTOR,
                    d.CREDITOR,
                })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => (r.AccountId, r.Value))
            .Select(g => new OpeningBalanceAccountLine(
                g.Key.AccountId, g.First().ACCCODE!, g.First().ACCCODENAME, g.Key.Value,
                g.Sum(x => (x.DEBTOR ?? 0) - (x.CREDITOR ?? 0))))
            .ToList();
    }
}
