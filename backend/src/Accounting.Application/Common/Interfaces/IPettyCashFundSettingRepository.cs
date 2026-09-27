using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side repository for <see cref="TB_PC_FUND_SETTING"/> ("تنظیمات تنخواه"), one of the
/// three new petty-cash side tables (<c>docs/tankhah-khazaneh-module.md</c> §3). Only stages
/// changes — never calls SaveChanges; the handler owns the transaction boundary via
/// <see cref="IUnitOfWork"/>.
/// </summary>
public interface IPettyCashFundSettingRepository
{
    Task AddAsync(TB_PC_FUND_SETTING setting, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the single change-tracked settings row for <paramref name="revolvingFundId"/>, or
    /// <see langword="null"/> when none exists yet (a fund with no settings configured is a valid,
    /// common state — <c>GET .../settings</c> reports it as 404 "not configured", per the
    /// frontend contract; <c>POST .../settings</c> upserts it). <paramref name="vahedCode"/> is
    /// not an ownership check on this row directly (the row may itself have a null/empty
    /// <c>VAHEDCODE</c>) — callers are expected to have already verified the parent fund's
    /// ownership via <see cref="IRevolvingFundRepository.GetForUpdateAsync"/> before calling this.
    /// </summary>
    Task<TB_PC_FUND_SETTING?> GetByFundIdAsync(Guid revolvingFundId, CancellationToken cancellationToken = default);
}
