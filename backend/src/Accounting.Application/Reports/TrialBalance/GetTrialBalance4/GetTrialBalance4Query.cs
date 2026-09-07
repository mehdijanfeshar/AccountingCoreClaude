using MediatR;

namespace Accounting.Application.Reports.TrialBalance.GetTrialBalance4;

/// <summary>
/// Runs the 4-column trial balance (تراز ۴ ستونی) aggregate query and returns one
/// <see cref="TrialBalance4RowDto"/> per distinct account code at <see cref="Level"/>. READ-ONLY —
/// no <c>IUnitOfWork</c> anywhere in this feature. No paging — see
/// <see cref="Common.Interfaces.ITrialBalanceReadRepository.GetAggregatesAsync"/> XML doc for why a
/// partial trial balance is meaningless.
///
/// <b>Performance note (documented, not solved here):</b> at <see cref="TrialBalanceLevel.Moin"/>
/// this can return one row per leaf account code that had any activity in the year, with no upper
/// bound enforced by the API — a genuinely large chart of accounts plus a genuinely long fiscal
/// year could produce a large unpaged response. Acceptable for now because a partial result would
/// be actively wrong (see above), not merely inconvenient; if this becomes a real problem the fix
/// belongs in caching/streaming the response, not in truncating the report.
/// </summary>
/// <param name="Year">Required <c>TB_VOUCHERSHEAD.YEAR</c> exact-match filter (4 chars). See
/// <see cref="GetTrialBalance4QueryValidator"/> XML doc for why this is mandatory.</param>
/// <param name="FromDate">Optional Jalali <c>YYYYMMDD</c> string — start of the reporting period
/// (exclusive on the opening side: activity strictly before this date is "opening", not
/// "period"). <see langword="null"/> means there is no opening window at all.</param>
/// <param name="ToDate">Optional Jalali <c>YYYYMMDD</c> string — end of the reporting period
/// (inclusive). <see langword="null"/> means no upper bound.</param>
/// <param name="VahedCode">Optional exact-match filter on <c>TB_VOUCHERSHEAD.VAHEDCODE</c>.</param>
/// <param name="Level">Which row of the coding hierarchy to aggregate by.</param>
/// <param name="DocLife">Optional inclusive lower bound on the raw <c>DOCLIFE</c> number (0..4).</param>
public sealed record GetTrialBalance4Query(
    string Year,
    string? FromDate,
    string? ToDate,
    string? VahedCode,
    TrialBalanceLevel Level,
    int? DocLife) : IRequest<IReadOnlyList<TrialBalance4RowDto>>;
