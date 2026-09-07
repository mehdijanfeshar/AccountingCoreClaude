using MediatR;

namespace Accounting.Application.Reports.TrialBalance.GetTrialBalance8;

/// <summary>
/// Runs the 8-column trial balance (تراز ۸ ستونی) aggregate query and returns one
/// <see cref="TrialBalance8RowDto"/> per distinct account code at <see cref="Level"/>. READ-ONLY —
/// no <c>IUnitOfWork</c> anywhere in this feature. No paging — see
/// <see cref="Common.Interfaces.ITrialBalanceReadRepository.GetAggregatesAsync"/> XML doc for why a
/// partial trial balance is meaningless. See <see cref="GetTrialBalance4.GetTrialBalance4Query"/>
/// for the same performance note at <see cref="TrialBalanceLevel.Moin"/> (applies identically here).
/// </summary>
/// <param name="Year">Required <c>TB_VOUCHERSHEAD.YEAR</c> exact-match filter (4 chars). See
/// <see cref="GetTrialBalance8QueryValidator"/> XML doc for why this is mandatory.</param>
/// <param name="FromDate">Optional Jalali <c>YYYYMMDD</c> string — start of the reporting period
/// (exclusive on the opening side). <see langword="null"/> means there is no opening window.</param>
/// <param name="ToDate">Optional Jalali <c>YYYYMMDD</c> string — end of the reporting period
/// (inclusive). <see langword="null"/> means no upper bound.</param>
/// <param name="VahedCode">Optional exact-match filter on <c>TB_VOUCHERSHEAD.VAHEDCODE</c>.</param>
/// <param name="Level">Which row of the coding hierarchy to aggregate by.</param>
/// <param name="DocLife">Optional inclusive lower bound on the raw <c>DOCLIFE</c> number (0..4).</param>
public sealed record GetTrialBalance8Query(
    string Year,
    string? FromDate,
    string? ToDate,
    string? VahedCode,
    TrialBalanceLevel Level,
    int? DocLife) : IRequest<IReadOnlyList<TrialBalance8RowDto>>;
