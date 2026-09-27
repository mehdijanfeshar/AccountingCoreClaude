using Accounting.Application.Reports.AccountReview;
using Accounting.Application.Reports.AccountReview.GetAccountReview;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read side of مرور حساب‌ها. Backed by the Oracle view <c>VW_CONSOLIDATE_REPORT</c> — the one
/// repository in this project that satisfies team working-rule #2 by construction rather than by
/// documented exception. (The views behind مرور اسناد and دفتر روزنامه were examined and rejected;
/// this one keeps one row per line and filters deleted vouchers in its own join.)
/// </summary>
public interface IAccountReviewReadRepository
{
    /// <summary>
    /// Returns one aggregated row per distinct code at the query's level, ordered by code, plus
    /// the resolved drill-down path and the levels that carry data inside it.
    ///
    /// <para>
    /// Rows whose code at that level is null are excluded — a تفصیلی level a line was never
    /// assigned at is absence, not a group.
    /// </para>
    /// </summary>
    Task<AccountReviewResultDto> GetAsync(
        GetAccountReviewQuery query,
        CancellationToken cancellationToken = default);
}
