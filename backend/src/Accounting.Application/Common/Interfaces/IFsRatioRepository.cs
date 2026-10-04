using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Interfaces;

/// <summary>نسبت‌های مالی (<c>TB_FS_RATIO</c>) — ح-۸. فیلتر مالکیت در Application.</summary>
public interface IFsRatioRepository
{
    Task<IReadOnlyList<TB_FS_RATIO>> GetAllAsync(FsFramework? framework, CancellationToken cancellationToken = default);

    Task<TB_FS_RATIO?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TB_FS_RATIO ratio, CancellationToken cancellationToken = default);
}
