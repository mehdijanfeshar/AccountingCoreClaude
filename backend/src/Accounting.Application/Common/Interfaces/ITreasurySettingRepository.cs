using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_TR_SETTING"/> ("تنظیمات خزانه‌داری") — خزانه‌داری، بخش
/// ۴-الف (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). One row per <c>VAHEDCODE</c> (UNIQUE); no
/// row exists until the unit's مدیر مالی defines one (no default anywhere). Only stages changes —
/// never calls SaveChanges; the handler owns the transaction boundary via <see cref="IUnitOfWork"/>.
/// </summary>
public interface ITreasurySettingRepository
{
    Task AddAsync(TB_TR_SETTING setting, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the single settings row for <paramref name="vahedCode"/>, change-tracked, or
    /// <see langword="null"/> when the unit has none yet.
    /// </summary>
    Task<TB_TR_SETTING?> GetForUpdateAsync(string vahedCode, CancellationToken cancellationToken = default);
}
