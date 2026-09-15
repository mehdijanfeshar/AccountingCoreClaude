using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
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
///
/// <see cref="VahedCode"/> is NOT an optional filter — via <see cref="IVahedScopedQuery"/>, every
/// call is unconditionally scoped to the caller's own organizational unit. This closes IDOR risk
/// #1 (CLAUDE.md) on what was the largest single data-leak surface in the project before this
/// change: an entire unit's trial balance was reachable by any authenticated caller via one
/// query-string parameter.
/// </summary>
/// <param name="Year">Required <c>TB_VOUCHERSHEAD.YEAR</c> exact-match filter (4 chars). See
/// <see cref="GetTrialBalance4QueryValidator"/> XML doc for why this is mandatory.</param>
/// <param name="FromDate">Optional Jalali <c>YYYYMMDD</c> string — start of the reporting period
/// (exclusive on the opening side: activity strictly before this date is "opening", not
/// "period"). <see langword="null"/> means there is no opening window at all.</param>
/// <param name="ToDate">Optional Jalali <c>YYYYMMDD</c> string — end of the reporting period
/// (inclusive). <see langword="null"/> means no upper bound.</param>
/// <param name="Level">Which row of the coding hierarchy to aggregate by.</param>
/// <param name="DocLife">Optional inclusive lower bound on the raw <c>DOCLIFE</c> number (0..4).</param>
public sealed record GetTrialBalance4Query(
    string Year,
    string? FromDate,
    string? ToDate,
    TrialBalanceLevel Level,
    int? DocLife) : IRequest<IReadOnlyList<TrialBalance4RowDto>>, IVahedScopedQuery
{
    /// <summary>
    /// Organizational unit code to filter by. Server-assigned by <c>VahedScopeBehavior</c> from
    /// the authenticated caller's own <c>VahedCode</c> — never bound from client input (the
    /// controller builds this query from individually-bound query-string parameters, not a
    /// deserialized body, but <see cref="JsonIgnoreAttribute"/> is still applied here for
    /// consistency with every other <see cref="IVahedScopedQuery"/>/<c>IVahedScopedCommand</c>
    /// implementer and to keep it out of the Swagger schema).
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
