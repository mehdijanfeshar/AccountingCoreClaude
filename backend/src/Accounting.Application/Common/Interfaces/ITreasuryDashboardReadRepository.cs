using Accounting.Application.Treasury.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Assembles <see cref="TreasuryDashboardDto"/> — خزانه‌داری، بخش ۴-د
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). A single composed read spanning
/// درخواست‌های پرداخت/دریافت‌ها/انتقال‌ها/تنخواه — kept as one bounded, per-unit, current-year
/// query rather than several controller round-trips, since every figure on the dashboard needs
/// the same "right now, this unit" snapshot.
/// </summary>
public interface ITreasuryDashboardReadRepository
{
    Task<TreasuryDashboardDto> GetAsync(string vahedCode, CancellationToken cancellationToken = default);
}
