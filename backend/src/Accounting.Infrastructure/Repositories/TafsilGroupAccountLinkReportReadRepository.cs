using Accounting.Application.Common.Interfaces;
using Accounting.Application.Reports.TafsilGroupAccountLinks;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class TafsilGroupAccountLinkReportReadRepository(LegacyDbContext db) : ITafsilGroupAccountLinkReportReadRepository
{
    public async Task<IReadOnlyList<TafsilGroupAccountLinkReportRowDto>> GetAsync(
        TafsilGroupAccountLinkFilter filter, CancellationToken cancellationToken = default)
    {
        var query =
            from l in db.TB_ACCOUNT_LINK_TAFSILGROUPs.AsNoTracking()
            join a in db.TB_ACCOUNTCODEs.AsNoTracking() on l.ACCOUNT_ID equals a.ID
            join lv in db.TB_LEVEL_TAFSILs.AsNoTracking() on l.LEVEL_ID equals lv.ID
            join g in db.TB_TAFSIL_GROUPs.AsNoTracking() on l.TAFSILGROUP_ID equals g.ID
            where !l.ISDELETED && a.ISDELETED != true && a.ACCCODE != null
            select new { l, a, lv, g };

        if (filter.AccountIds is { Count: > 0 } accountIds)
            query = query.Where(x => accountIds.Contains(x.a.ID));
        if (filter.LevelId is { } levelId)
            query = query.Where(x => x.lv.ID == levelId);
        if (filter.TafsilGroupId is { } groupId)
            query = query.Where(x => x.g.ID == groupId);
        if (filter.TafsiliId is { } tafsiliId)
        {
            query = query.Where(x => db.TB_TAFSIL_LINK_TAFSILGROUPs
                .Any(tl => !tl.ISDELETED && tl.TAFSILGROUP_ID == x.g.ID && tl.TAFSIL_ID == tafsiliId));
        }

        return await query
            .OrderBy(x => x.a.ACCCODE).ThenBy(x => x.lv.LEVEL_CODE).ThenBy(x => x.g.TAFSILGROUP_CODE)
            .Select(x => new TafsilGroupAccountLinkReportRowDto(
                x.a.ID, x.a.ACCCODE!, x.a.ACCCODENAME,
                x.lv.ID, x.lv.LEVEL_NAME,
                x.g.ID, x.g.TAFSILGROUP_CODE, x.g.TAFSILGROUP_NAME))
            .ToListAsync(cancellationToken);
    }
}
