using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>دسترسی‌های صورت‌های مالی (<c>TB_FS_PERMISSION</c>) — ط-۲.</summary>
public interface IFsPermissionRepository
{
    /// <summary>همهٔ ردیف‌های حذف‌نشده، بدون tracking.</summary>
    Task<IReadOnlyList<TB_FS_PERMISSION>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TB_FS_PERMISSION?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TB_FS_PERMISSION row, CancellationToken cancellationToken = default);
}
