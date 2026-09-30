using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// قواعد کنترل تساوی بین صورت‌ها (<c>TB_FS_CHECK_RULE</c>) — بخش ۴۵-ه. جدول کوچک است؛ فیلتر مالکیت
/// (مشترک/اختصاصی، <c>FsUnitScope</c>) در Application و در حافظه. نوشتن فقط stage می‌کند.
/// </summary>
public interface IFsCheckRuleRepository
{
    /// <summary>همهٔ قواعد حذف‌نشده (اختیاری یک مجموعه)، بدون tracking.</summary>
    Task<IReadOnlyList<TB_FS_CHECK_RULE>> GetAllAsync(FsFramework? framework, CancellationToken cancellationToken = default);

    Task<TB_FS_CHECK_RULE?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>کد در همان مالک و مجموعه (حذف‌شده هم) — یکتایی <c>UK_FS_CHECK_RULE</c>.</summary>
    Task<bool> CodeExistsAsync(string? ownerVahedCode, FsFramework framework, string code, CancellationToken cancellationToken = default);

    Task AddAsync(TB_FS_CHECK_RULE rule, CancellationToken cancellationToken = default);
}
