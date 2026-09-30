using Accounting.Application.FinancialStatements;
using Accounting.Application.FinancialStatements.Queries;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository for the financial-statement templates (<c>TB_FS_TEMPLATE</c>/
/// <c>_VERSION</c>/<c>_ROW</c>) — فاز ۴۵ (<c>docs/fs-module.md</c> §۸). هر خواندن با <see cref="FsUnitScope"/>
/// واحد هدر فیلتر می‌شود: فقط قالب‌های مشترک، اجداد، خود و زیرمجموعه. Never stages changes.
/// </summary>
public interface IFsTemplateReadRepository
{
    /// <summary>قالب‌های دیدنی برای <paramref name="scope"/> (اختیاری یک مجموعه)، به ترتیب مجموعه و <c>ORDER_NO</c>.</summary>
    Task<IReadOnlyList<FsTemplateDto>> GetTemplatesAsync(FsFramework? framework, FsUnitScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// یک نسخهٔ حذف‌نشده با ردیف‌هایش، یا <see langword="null"/> اگر نیست یا برای <paramref name="scope"/>
    /// دیدنی نیست. <paramref name="scope"/> خالی = بدون فیلتر (فقط مصرف داخلی).
    /// </summary>
    Task<FsTemplateVersionDetailDto?> GetVersionAsync(Guid versionId, FsUnitScope? scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// نسخهٔ قالب هر صورت مجموعهٔ <paramref name="framework"/> برای اجرای سال <paramref name="year"/> در
    /// واحد <paramref name="scope"/>: به‌ازای هر کد قالب، قالبِ نزدیک‌ترین مالک در زنجیرهٔ خود ← اجداد، و
    /// اگر نبود قالب مشترک (فقط قالبی که نسخهٔ قابل‌استفاده دارد). نسخه: فعال با بزرگ‌ترین
    /// <c>EFFECTIVE_FROM_YEAR &lt;= year</c>؛ اگر <paramref name="useDrafts"/>، پیش‌نویس باز مقدم است.
    /// به ترتیب <c>ORDER_NO</c> قالب.
    /// </summary>
    Task<IReadOnlyList<FsTemplateVersionDetailDto>> GetVersionsForRunAsync(
        FsFramework framework, int year, bool useDrafts, FsUnitScope scope, CancellationToken cancellationToken = default);

    /// <summary>Codes of every non-deleted template — for validating <c>STMT(...)</c> references.</summary>
    Task<IReadOnlySet<string>> GetTemplateCodesAsync(CancellationToken cancellationToken = default);
}
