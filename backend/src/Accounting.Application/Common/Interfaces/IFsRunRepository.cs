using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

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

    /// <summary>بخش ۴۵-ه — ثبت یک قدم گردش تأیید (فقط درج).</summary>
    Task AddActionAsync(TB_FS_RUN_ACTION action, CancellationToken cancellationToken = default);

    /// <summary>ح-۵ — آخرین اجرای حذف‌نشده و جایگزین‌نشدهٔ هر واحد در یک سال (برای صفحهٔ بستن دوره).</summary>
    Task<IReadOnlyList<FsUnitRunStatusDto>> GetLatestRunsAsync(IReadOnlyCollection<string> vahedCodes, string year, CancellationToken cancellationToken = default);

    /// <summary>ح-۹ — کنترل‌های ناموفقِ ارجاع‌شدهٔ باز به یک کاربر، در اجراهای حذف‌نشدهٔ یک واحد.</summary>
    Task<IReadOnlyList<FsAssignedCheckDto>> GetOpenAssignmentsAsync(string vahedCode, string userId, CancellationToken cancellationToken = default);

    /// <summary>ح-۳ — درج «نظر».</summary>
    Task AddCommentAsync(TB_FS_RUN_COMMENT comment, CancellationToken cancellationToken = default);

    /// <summary>ح-۳ — نظرهای حذف‌نشدهٔ اجرا، قدیمی‌ترین اول.</summary>
    Task<IReadOnlyList<TB_FS_RUN_COMMENT>> GetCommentsAsync(Guid runId, string vahedCode, CancellationToken cancellationToken = default);

    Task<TB_FS_RUN_COMMENT?> GetCommentForUpdateAsync(Guid runId, Guid commentId, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>ح-۳ — یک کنترل اجرا برای ارجاع / رفع.</summary>
    Task<TB_FS_RUN_CHECK?> GetCheckForUpdateAsync(Guid runId, Guid checkId, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// بخش ۴۵-ه — اجراهای منتشرشدهٔ همان «دوره»ٔ واحد (مجموعه، سال، ماه پایان، ترکیبی/جداگانه) برای جایگزینی
    /// هنگام انتشار نسخهٔ تازه؛ change-tracked.
    /// </summary>
    Task<IReadOnlyList<TB_FS_RUN>> GetPublishedForUpdateAsync(
        string vahedCode, FsFramework framework, string year, int toMonth, bool includeSubUnits, CancellationToken cancellationToken = default);

    /// <summary>بخش ۴۵-د — ردیف <paramref name="rowId"/> از اجرای <paramref name="runId"/> واحد هدر، یا null.</summary>
    Task<FsDrillTarget?> GetDrillTargetAsync(Guid runId, Guid rowId, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>سهم هر معین در ردیف (جمع زیرواحدها)، از <c>TB_FS_RUN_ACCOUNT</c>.</summary>
    Task<IReadOnlyList<FsDrillAccountDto>> GetRowAccountsAsync(Guid runId, Guid rowId, string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>سهم هر زیرواحد در ردیف، یا فقط در معین <paramref name="accCode"/>؛ نام واحد خالی (فراخوان پر می‌کند).</summary>
    Task<IReadOnlyList<FsDrillUnitDto>> GetRowUnitsAsync(
        Guid runId, Guid rowId, string vahedCode, string? accCode, CancellationToken cancellationToken = default);
}
