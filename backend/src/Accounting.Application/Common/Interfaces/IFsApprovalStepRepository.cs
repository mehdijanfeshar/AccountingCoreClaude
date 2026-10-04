using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// مراحل گردش تأیید صورت‌ها (<c>TB_FS_APPROVAL_STEP</c>) — ح-۴. فیلتر دید/مالکیت (مشترک/اختصاصی،
/// <c>FsUnitScope</c>) در Application و در حافظه. نوشتن فقط stage می‌کند.
/// </summary>
public interface IFsApprovalStepRepository
{
    /// <summary>همهٔ مراحل حذف‌نشده (اختیاری یک مجموعه)، بدون tracking.</summary>
    Task<IReadOnlyList<TB_FS_APPROVAL_STEP>> GetAllAsync(FsFramework? framework, CancellationToken cancellationToken = default);

    Task<TB_FS_APPROVAL_STEP?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TB_FS_APPROVAL_STEP step, CancellationToken cancellationToken = default);
}
