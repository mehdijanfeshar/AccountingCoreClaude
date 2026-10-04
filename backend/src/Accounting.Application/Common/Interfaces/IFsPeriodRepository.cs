using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>وضعیت دورهٔ صورت‌ها (<c>TB_FS_PERIOD</c> + لاگ) — ح-۵. نوشتن فقط stage می‌کند.</summary>
public interface IFsPeriodRepository
{
    /// <summary>همهٔ ردیف‌های وضعیت یک سال (همهٔ واحدها)، بدون tracking.</summary>
    Task<IReadOnlyList<TB_FS_PERIOD>> GetForYearAsync(string year, CancellationToken cancellationToken = default);

    Task<TB_FS_PERIOD?> GetForUpdateAsync(string vahedCode, string year, CancellationToken cancellationToken = default);

    Task AddAsync(TB_FS_PERIOD period, CancellationToken cancellationToken = default);

    Task AddLogAsync(TB_FS_PERIOD_LOG log, CancellationToken cancellationToken = default);

    /// <summary>لاگ یک واحد در یک سال، جدیدترین اول.</summary>
    Task<IReadOnlyList<TB_FS_PERIOD_LOG>> GetLogsAsync(string vahedCode, string year, CancellationToken cancellationToken = default);
}
