using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// ط-۳ تا ط-۸ — تنظیمات مجموعه، قواعد حذف، شرکت‌های تابعه (تراز و نرخ)، نگاشت XBRL و داده‌های جانبی هر اجرا
/// (درخت واحد، کاربرگ، نتیجهٔ حذف). خواندن‌ها بدون tracking؛ ForUpdate با tracking؛ نوشتن فقط stage.
/// </summary>
public interface IFsConsolidationRepository
{
    Task<IReadOnlyList<TB_FS_SETTING>> GetSettingsAsync(FsFramework? framework, CancellationToken cancellationToken = default);

    Task<TB_FS_SETTING?> GetSettingForUpdateAsync(string? ownerVahedCode, FsFramework framework, string key, CancellationToken cancellationToken = default);

    Task AddSettingAsync(TB_FS_SETTING setting, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TB_FS_ELIM_RULE>> GetElimRulesAsync(FsFramework? framework, CancellationToken cancellationToken = default);

    Task<TB_FS_ELIM_RULE?> GetElimRuleForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddElimRuleAsync(TB_FS_ELIM_RULE rule, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TB_FS_ENTITY>> GetEntitiesAsync(CancellationToken cancellationToken = default);

    Task<TB_FS_ENTITY?> GetEntityForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddEntityAsync(TB_FS_ENTITY entity, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TB_FS_ENTITY_TB>> GetEntityTbAsync(IReadOnlyCollection<Guid> entityIds, string year, int toMonth, CancellationToken cancellationToken = default);

    /// <summary>تراز یک شرکت برای یک دوره را کامل جایگزین می‌کند (حذف سخت قبلی + درج).</summary>
    Task ReplaceEntityTbAsync(Guid entityId, string year, int toMonth, IEnumerable<TB_FS_ENTITY_TB> rows, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TB_FS_ENTITY_RATE>> GetRatesAsync(IReadOnlyCollection<Guid> entityIds, string year, int toMonth, CancellationToken cancellationToken = default);

    Task<TB_FS_ENTITY_RATE?> GetRateForUpdateAsync(Guid entityId, string year, int toMonth, CancellationToken cancellationToken = default);

    Task AddRateAsync(TB_FS_ENTITY_RATE rate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TB_FS_XBRL_MAP>> GetXbrlMapsAsync(CancellationToken cancellationToken = default);

    Task<TB_FS_XBRL_MAP?> GetXbrlMapForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddXbrlMapAsync(TB_FS_XBRL_MAP map, CancellationToken cancellationToken = default);

    void RemoveXbrlMap(TB_FS_XBRL_MAP map);

    Task AddRunExtrasAsync(
        IEnumerable<TB_FS_RUN_UNIT> units, IEnumerable<TB_FS_RUN_ROW_GROUP> groups, IEnumerable<TB_FS_RUN_ELIM> elims, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TB_FS_RUN_UNIT>> GetRunUnitsAsync(Guid runId, string vahedCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TB_FS_RUN_ROW_GROUP>> GetRunRowGroupsAsync(Guid runId, string vahedCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TB_FS_RUN_ELIM>> GetRunElimsAsync(Guid runId, string vahedCode, CancellationToken cancellationToken = default);
}
