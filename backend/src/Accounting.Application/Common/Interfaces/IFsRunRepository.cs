using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// اجراهای تهیهٔ صورت‌های مالی (<c>TB_FS_RUN</c> + Snapshot) — فاز ۴۵-ب. نوشتن فقط درج گراف کامل
/// اجرا و حذف نرم است؛ Snapshot هرگز به‌روز نمی‌شود. همهٔ خواندن‌ها با <c>VAHEDCODE</c> فیلتر می‌شوند
/// (اجرای واحد دیگر = ۴۰۴، نه ۴۰۳ — وجودش هم لو نمی‌رود).
/// </summary>
public interface IFsRunRepository
{
    /// <summary>درج اجرا با همهٔ صورت‌ها، ردیف‌ها و ترکیب حساب‌ها (گراف navigation).</summary>
    Task AddAsync(TB_FS_RUN run, CancellationToken cancellationToken = default);

    /// <summary>بزرگ‌ترین <c>RUN_NO</c> موجود + ۱.</summary>
    Task<int> GetNextRunNoAsync(CancellationToken cancellationToken = default);

    Task<TB_FS_RUN?> GetForUpdateAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FsRunSummaryDto>> ListAsync(string vahedCode, string? year, CancellationToken cancellationToken = default);

    Task<FsRunDetailDto?> GetDetailAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);
}
