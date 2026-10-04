using Accounting.Application.Common.Interfaces;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;
using Accounting.Infrastructure.Legacy;
using Microsoft.EntityFrameworkCore;

namespace Accounting.Infrastructure.Repositories;

public sealed class FsConsolidationRepository : IFsConsolidationRepository
{
    private readonly LegacyDbContext _db;

    public FsConsolidationRepository(LegacyDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TB_FS_SETTING>> GetSettingsAsync(FsFramework? framework, CancellationToken cancellationToken = default)
    {
        var q = _db.TB_FS_SETTINGs.AsNoTracking();

        if (framework is { } fw)
        {
            q = q.Where(s => s.FRAMEWORK == fw);
        }

        return await q.ToListAsync(cancellationToken);
    }

    public Task<TB_FS_SETTING?> GetSettingForUpdateAsync(string? ownerVahedCode, FsFramework framework, string key, CancellationToken cancellationToken = default)
    {
        var q = _db.TB_FS_SETTINGs.Where(s => s.FRAMEWORK == framework && s.SETTING_KEY == key);
        q = ownerVahedCode is null ? q.Where(s => s.VAHEDCODE == null) : q.Where(s => s.VAHEDCODE == ownerVahedCode);
        return q.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddSettingAsync(TB_FS_SETTING setting, CancellationToken cancellationToken = default)
        => await _db.TB_FS_SETTINGs.AddAsync(setting, cancellationToken);

    public async Task<IReadOnlyList<TB_FS_ELIM_RULE>> GetElimRulesAsync(FsFramework? framework, CancellationToken cancellationToken = default)
    {
        var q = _db.TB_FS_ELIM_RULEs.AsNoTracking().Where(r => !r.ISDELETED);

        if (framework is { } fw)
        {
            q = q.Where(r => r.FRAMEWORK == fw);
        }

        return await q.OrderBy(r => r.CODE).ToListAsync(cancellationToken);
    }

    public Task<TB_FS_ELIM_RULE?> GetElimRuleForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.TB_FS_ELIM_RULEs.FirstOrDefaultAsync(r => r.ID == id && !r.ISDELETED, cancellationToken);

    public async Task AddElimRuleAsync(TB_FS_ELIM_RULE rule, CancellationToken cancellationToken = default)
        => await _db.TB_FS_ELIM_RULEs.AddAsync(rule, cancellationToken);

    public async Task<IReadOnlyList<TB_FS_ENTITY>> GetEntitiesAsync(CancellationToken cancellationToken = default)
        => await _db.TB_FS_ENTITYs.AsNoTracking().Where(e => !e.ISDELETED).OrderBy(e => e.CODE).ToListAsync(cancellationToken);

    public Task<TB_FS_ENTITY?> GetEntityForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.TB_FS_ENTITYs.FirstOrDefaultAsync(e => e.ID == id && !e.ISDELETED, cancellationToken);

    public async Task AddEntityAsync(TB_FS_ENTITY entity, CancellationToken cancellationToken = default)
        => await _db.TB_FS_ENTITYs.AddAsync(entity, cancellationToken);

    public async Task<IReadOnlyList<TB_FS_ENTITY_TB>> GetEntityTbAsync(IReadOnlyCollection<Guid> entityIds, string year, int toMonth, CancellationToken cancellationToken = default)
        => entityIds.Count == 0
            ? []
            : await _db.TB_FS_ENTITY_TBs.AsNoTracking()
                .Where(t => entityIds.Contains(t.ENTITY_ID) && t.YEAR == year && t.TO_MONTH == toMonth)
                .ToListAsync(cancellationToken);

    public async Task ReplaceEntityTbAsync(Guid entityId, string year, int toMonth, IEnumerable<TB_FS_ENTITY_TB> rows, CancellationToken cancellationToken = default)
    {
        var old = await _db.TB_FS_ENTITY_TBs.Where(t => t.ENTITY_ID == entityId && t.YEAR == year && t.TO_MONTH == toMonth).ToListAsync(cancellationToken);
        _db.TB_FS_ENTITY_TBs.RemoveRange(old);
        await _db.TB_FS_ENTITY_TBs.AddRangeAsync(rows, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_FS_ENTITY_RATE>> GetRatesAsync(IReadOnlyCollection<Guid> entityIds, string year, int toMonth, CancellationToken cancellationToken = default)
        => entityIds.Count == 0
            ? []
            : await _db.TB_FS_ENTITY_RATEs.AsNoTracking()
                .Where(r => entityIds.Contains(r.ENTITY_ID) && r.YEAR == year && r.TO_MONTH == toMonth)
                .ToListAsync(cancellationToken);

    public Task<TB_FS_ENTITY_RATE?> GetRateForUpdateAsync(Guid entityId, string year, int toMonth, CancellationToken cancellationToken = default)
        => _db.TB_FS_ENTITY_RATEs.FirstOrDefaultAsync(r => r.ENTITY_ID == entityId && r.YEAR == year && r.TO_MONTH == toMonth, cancellationToken);

    public async Task AddRateAsync(TB_FS_ENTITY_RATE rate, CancellationToken cancellationToken = default)
        => await _db.TB_FS_ENTITY_RATEs.AddAsync(rate, cancellationToken);

    public async Task<IReadOnlyList<TB_FS_XBRL_MAP>> GetXbrlMapsAsync(CancellationToken cancellationToken = default)
        => await _db.TB_FS_XBRL_MAPs.AsNoTracking().OrderBy(m => m.TEMPLATE_CODE).ThenBy(m => m.ROW_CODE).ToListAsync(cancellationToken);

    public Task<TB_FS_XBRL_MAP?> GetXbrlMapForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.TB_FS_XBRL_MAPs.FirstOrDefaultAsync(m => m.ID == id, cancellationToken);

    public async Task AddXbrlMapAsync(TB_FS_XBRL_MAP map, CancellationToken cancellationToken = default)
        => await _db.TB_FS_XBRL_MAPs.AddAsync(map, cancellationToken);

    public void RemoveXbrlMap(TB_FS_XBRL_MAP map) => _db.TB_FS_XBRL_MAPs.Remove(map);

    public async Task AddRunExtrasAsync(
        IEnumerable<TB_FS_RUN_UNIT> units, IEnumerable<TB_FS_RUN_ROW_GROUP> groups, IEnumerable<TB_FS_RUN_ELIM> elims, CancellationToken cancellationToken = default)
    {
        await _db.TB_FS_RUN_UNITs.AddRangeAsync(units, cancellationToken);
        await _db.TB_FS_RUN_ROW_GROUPs.AddRangeAsync(groups, cancellationToken);
        await _db.TB_FS_RUN_ELIMs.AddRangeAsync(elims, cancellationToken);
    }

    public async Task<IReadOnlyList<TB_FS_RUN_UNIT>> GetRunUnitsAsync(Guid runId, string vahedCode, CancellationToken cancellationToken = default)
        => await _db.TB_FS_RUN_UNITs.AsNoTracking().Where(u => u.RUN_ID == runId && u.VAHEDCODE == vahedCode).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TB_FS_RUN_ROW_GROUP>> GetRunRowGroupsAsync(Guid runId, string vahedCode, CancellationToken cancellationToken = default)
        => await _db.TB_FS_RUN_ROW_GROUPs.AsNoTracking().Where(g => g.RUN_ID == runId && g.VAHEDCODE == vahedCode).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TB_FS_RUN_ELIM>> GetRunElimsAsync(Guid runId, string vahedCode, CancellationToken cancellationToken = default)
        => await _db.TB_FS_RUN_ELIMs.AsNoTracking().Where(e => e.RUN_ID == runId && e.VAHEDCODE == vahedCode).ToListAsync(cancellationToken);
}
