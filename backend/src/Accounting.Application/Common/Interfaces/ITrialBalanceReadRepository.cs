using Accounting.Application.Reports.TrialBalance;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository backing all three trial balance reports (4/6/8-column). Deliberately a
/// single method shared by all three — the underlying aggregate query is identical regardless of
/// how many columns the caller ultimately wants; only the Query handlers differ in which of
/// <see cref="TrialBalanceAggregateRow"/>'s properties they project into their public DTO.
///
/// This is READ-ONLY reporting: no write repository, no <c>IUnitOfWork</c> dependency anywhere in
/// this feature, and this method never stages or persists anything.
///
/// This is the one Query in the codebase whose Read side cannot reasonably use plain EF Core LINQ
/// (see <c>TrialBalanceReadRepository</c> XML doc for why — <c>TB_ACCOUNTCODE.TYPECODE</c> and
/// <c>TB_VOUCHERSHEAD.DOCLIFE</c> are mapped to <c>bool?</c> but the report needs to compare them
/// as numbers). It still honours rule #2 in <c>CLAUDE.md</c> ("سمت Read باید از View/Materialized
/// View مجزا بخواند... نه مستقیماً از مدل نوشتن") in spirit as far as this schema allows: it reads
/// the same base tables the write model uses (there is no View for this yet), exactly like the
/// pre-existing narrow exception already taken by <c>VoucherHeadReadRepository</c>/
/// <c>VoucherDetailReadRepository</c> — see their XML docs. It does NOT use Dapper: the project
/// intentionally has no Dapper dependency, and EF Core's <c>Database.SqlQueryRaw&lt;T&gt;</c> is
/// sufficient for a single hand-parameterized aggregate query.
/// </summary>
public interface ITrialBalanceReadRepository
{
    /// <summary>
    /// Runs the trial balance aggregate query and returns one <see cref="TrialBalanceAggregateRow"/>
    /// per distinct account code at <paramref name="level"/>, ordered by that code ascending.
    /// Always returns a fully materialized list (never <see cref="IQueryable{T}"/>) — deliberately,
    /// so this interface cannot be (mis)used to leak query composition into a Query handler, unlike
    /// a reference implementation this project consulted that returned <c>IQueryable</c> straight
    /// out of its repository.
    ///
    /// No paging parameter exists on purpose: a partial trial balance does not reconcile (its
    /// debtor/creditor totals would not actually balance against each other), so returning a
    /// "page" of one would be meaningless to the caller. See the three Query XML docs for the
    /// resulting performance consideration at <see cref="TrialBalanceLevel.Moin"/>.
    /// </summary>
    /// <param name="level">Which row of the coding hierarchy to aggregate by (Group/Kol/Moin).</param>
    /// <param name="year">Required <c>TB_VOUCHERSHEAD.YEAR</c> exact-match filter — see
    /// <c>GetTrialBalance4QueryValidator</c> XML doc for why this is mandatory rather than
    /// optional.</param>
    /// <param name="fromDate">Optional inclusive-lower-bound-minus-one Jalali <c>YYYYMMDD</c>
    /// string (opening window starts strictly before this date). <see langword="null"/> means "no
    /// opening window" — every voucher line is treated as falling inside the period.</param>
    /// <param name="toDate">Optional inclusive-upper-bound Jalali <c>YYYYMMDD</c> string.
    /// <see langword="null"/> means "no upper bound" — every voucher line up to the present is
    /// included in the cumulative/period windows.</param>
    /// <param name="vahedCode">
    /// Organizational unit code to filter by — required, not nullable. Server-assigned by
    /// <c>VahedScopeBehavior</c> from the authenticated caller's own unit code (via
    /// <c>GetTrialBalance4/6/8Query</c>'s <see cref="Accounting.Application.Common.Security.IVahedScopedQuery"/>
    /// implementation) — never caller input. Rows are matched with exact equality only
    /// (<c>h.VAHEDCODE = :vahedCode</c>); a head with <c>VAHEDCODE IS NULL</c> is never included
    /// in anyone's trial balance, by deliberate fail-closed design (see
    /// <c>TrialBalanceReadRepository</c> XML doc). This closes IDOR risk #1 (CLAUDE.md) on what was
    /// previously the largest single data-leak surface in the project: an entire unit's trial
    /// balance was reachable by any authenticated caller via one query-string parameter.
    /// </param>
    /// <param name="docLife">Optional inclusive lower bound on the raw
    /// <c>TB_VOUCHERSHEAD.DOCLIFE</c> number (read as a number here specifically to route around
    /// the <c>bool?</c> mapping bug — see the class remarks). <see langword="null"/> means no
    /// filtering by document life-cycle status at all.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<TrialBalanceAggregateRow>> GetAggregatesAsync(
        TrialBalanceLevel level,
        string year,
        string? fromDate,
        string? toDate,
        string vahedCode,
        int? docLife,
        CancellationToken cancellationToken = default);
}
