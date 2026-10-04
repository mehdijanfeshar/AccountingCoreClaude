using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>یادداشت‌های توضیحی متنی (<c>TB_FS_NARRATIVE</c> + نسخه‌ها + کپی اجرا) — ح-۶. نوشتن فقط stage می‌کند.</summary>
public interface IFsNarrativeRepository
{
    /// <summary>یادداشت‌های حذف‌نشدهٔ یک واحد/مجموعه/سال به ترتیب؛ <paramref name="tracking"/> برای ویرایش گروهی.</summary>
    Task<IReadOnlyList<TB_FS_NARRATIVE>> GetSetAsync(string vahedCode, FsFramework framework, string year, bool tracking, CancellationToken cancellationToken = default);

    Task<TB_FS_NARRATIVE?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TB_FS_NARRATIVE narrative, CancellationToken cancellationToken = default);

    Task AddVersionAsync(TB_FS_NARRATIVE_VERSION version, CancellationToken cancellationToken = default);

    /// <summary>نسخه‌ها، جدیدترین اول.</summary>
    Task<IReadOnlyList<TB_FS_NARRATIVE_VERSION>> GetVersionsAsync(Guid narrativeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TB_FS_RUN_NARRATIVE>> GetRunNarrativesAsync(Guid runId, string vahedCode, CancellationToken cancellationToken = default);

    Task AddRunNarrativesAsync(IEnumerable<TB_FS_RUN_NARRATIVE> rows, CancellationToken cancellationToken = default);
}
