using Accounting.Application.Common.Search;
using Accounting.Application.Reports.MatrixReport;
using Accounting.Application.Reports.MatrixReport.GetMatrixReport;
using Accounting.Application.Reports.TrialBalance;
using Accounting.Application.Reports.TrialBalance.GetTrialBalance4;
using Accounting.Application.Reports.TrialBalance.GetTrialBalance6;
using Accounting.Application.Reports.TrialBalance.GetTrialBalance8;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the trial balance (تراز آزمایشی) reports — 4/6/8-column variants.
/// Routed at <c>api/reports</c>. This is a READ-ONLY reporting feature: there is no matching write
/// controller, no Command, and no entity mutation anywhere behind these three actions — see
/// <see cref="Application.Common.Interfaces.ITrialBalanceReadRepository"/> and
/// <see cref="Infrastructure.Repositories.TrialBalanceReadRepository"/> XML docs for the full
/// design rationale (raw SQL, the Total = Opening + Period invariant, and the two reference-project
/// bugs deliberately fixed here).
///
/// <c>GET</c> only, like every read endpoint in this API — no <c>PUT</c>/<c>DELETE</c> anywhere
/// (enforced repo-wide by <c>HttpVerbConventionTests</c>). Every action does nothing but: build a
/// Query from the query string → send it through MediatR → wrap the result in <see cref="OkObjectResult"/>.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>), exactly like every
/// other controller except <c>HealthController</c>.
///
/// <b>All three actions also declare <c>403 Forbidden</c></b> (2026-09 IDOR closure) —
/// <c>GetTrialBalance4/6/8Query</c> all implement <c>IVahedScopedQuery</c>, so
/// <c>VahedScopeBehavior</c> throws <c>MissingVahedScopeException</c> → 403 (via
/// <c>GlobalExceptionHandler</c>) when the authenticated caller has no usable unit-scope claim.
/// There is also no <c>vahedCode</c> query parameter on any of the three actions anymore — before
/// this change, <c>vahedCode</c> was caller-supplied and this was the single largest data-leak
/// surface in the project (an entire unit's trial balance reachable via one query-string
/// parameter); the unit scope is now always the caller's own.
///
/// <b>No paging on any of the three actions</b> — a partial trial balance does not reconcile (its
/// debtor/creditor totals would not actually balance against each other), so returning a "page" of
/// rows would be actively misleading rather than merely incomplete. See
/// <see cref="Application.Common.Interfaces.ITrialBalanceReadRepository.GetAggregatesAsync"/> XML
/// doc for the resulting performance consideration at <see cref="TrialBalanceLevel.Moin"/>.
/// </summary>
[ApiController]
[Route("api/reports")]
public sealed class TrialBalanceReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TrialBalanceReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns the 4-column trial balance (تراز ۴ ستونی): one row per account code at
    /// <paramref name="level"/>, with period debtor/creditor turnover netted into a one-sided
    /// closing balance. See <see cref="TrialBalance4RowDto"/> for the exact column semantics.
    /// </summary>
    /// <param name="year">Required <c>TB_VOUCHERSHEAD.YEAR</c> exact-match filter (4 chars).</param>
    /// <param name="fromDate">Optional Jalali <c>YYYYMMDD</c> start of the reporting period.</param>
    /// <param name="toDate">Optional Jalali <c>YYYYMMDD</c> end of the reporting period.</param>
    /// <param name="level">Which row of the coding hierarchy to aggregate by.</param>
    /// <param name="docLife">Optional inclusive lower bound on the raw <c>DOCLIFE</c> number (0..4).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filters">Optional generic report filters (<c>Property</c>/<c>Operator</c>/<c>Value</c>),
    /// bound from the query string as <c>filters[0].property=code&amp;filters[0].operator=LIKE&amp;filters[0].value=11</c>.
    /// <c>Property</c> must name a field of <c>TrialBalanceSearchFields</c>; anything else is a 400.</param>
    [HttpGet("trial-balance-4")]
    [ProducesResponseType(typeof(IReadOnlyList<TrialBalance4RowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTrialBalance4(
        [FromQuery] string year = "",
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] TrialBalanceLevel level = default,
        [FromQuery] int? docLife = null,
        [FromQuery] List<SearchParam>? filters = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetTrialBalance4Query(year, fromDate, toDate, level, docLife, filters),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns the 6-column trial balance (تراز ۶ ستونی): adds the opening (pre-period, raw
    /// unnetted) turnover to every row of <see cref="GetTrialBalance4"/>. See
    /// <see cref="TrialBalance6RowDto"/> for the exact column semantics, including the important
    /// netting caveat on <c>FirstDebtor</c>/<c>FirstCreditor</c>.
    /// </summary>
    /// <param name="year">Required <c>TB_VOUCHERSHEAD.YEAR</c> exact-match filter (4 chars).</param>
    /// <param name="fromDate">Optional Jalali <c>YYYYMMDD</c> start of the reporting period.</param>
    /// <param name="toDate">Optional Jalali <c>YYYYMMDD</c> end of the reporting period.</param>
    /// <param name="level">Which row of the coding hierarchy to aggregate by.</param>
    /// <param name="docLife">Optional inclusive lower bound on the raw <c>DOCLIFE</c> number (0..4).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filters">Optional generic report filters (<c>Property</c>/<c>Operator</c>/<c>Value</c>),
    /// bound from the query string as <c>filters[0].property=code&amp;filters[0].operator=LIKE&amp;filters[0].value=11</c>.
    /// <c>Property</c> must name a field of <c>TrialBalanceSearchFields</c>; anything else is a 400.</param>
    [HttpGet("trial-balance-6")]
    [ProducesResponseType(typeof(IReadOnlyList<TrialBalance6RowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTrialBalance6(
        [FromQuery] string year = "",
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] TrialBalanceLevel level = default,
        [FromQuery] int? docLife = null,
        [FromQuery] List<SearchParam>? filters = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetTrialBalance6Query(year, fromDate, toDate, level, docLife, filters),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns the 8-column trial balance (تراز ۸ ستونی): adds the cumulative (opening + period)
    /// turnover to every row of <see cref="GetTrialBalance6"/>. See <see cref="TrialBalance8RowDto"/>
    /// for the exact column semantics — note that <c>TotDebtor</c>/<c>TotCreditor</c> are raw
    /// (unnetted) cumulative turnover, while <c>FirstDebtor</c>/<c>FirstCreditor</c> are a netted
    /// one-sided opening balance, so they do NOT reconcile via plain addition.
    /// </summary>
    /// <param name="year">Required <c>TB_VOUCHERSHEAD.YEAR</c> exact-match filter (4 chars).</param>
    /// <param name="fromDate">Optional Jalali <c>YYYYMMDD</c> start of the reporting period.</param>
    /// <param name="toDate">Optional Jalali <c>YYYYMMDD</c> end of the reporting period.</param>
    /// <param name="level">Which row of the coding hierarchy to aggregate by.</param>
    /// <param name="docLife">Optional inclusive lower bound on the raw <c>DOCLIFE</c> number (0..4).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="filters">Optional generic report filters (<c>Property</c>/<c>Operator</c>/<c>Value</c>),
    /// bound from the query string as <c>filters[0].property=code&amp;filters[0].operator=LIKE&amp;filters[0].value=11</c>.
    /// <c>Property</c> must name a field of <c>TrialBalanceSearchFields</c>; anything else is a 400.</param>
    [HttpGet("trial-balance-8")]
    [ProducesResponseType(typeof(IReadOnlyList<TrialBalance8RowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTrialBalance8(
        [FromQuery] string year = "",
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] TrialBalanceLevel level = default,
        [FromQuery] int? docLife = null,
        [FromQuery] List<SearchParam>? filters = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetTrialBalance8Query(year, fromDate, toDate, level, docLife, filters),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// گزارش ماتریسی (تلفیقی) — aggregates voucher activity at one level of the coding hierarchy
    /// (گروه/کل/معین) or at one of the seven تفصیلی levels, chosen by <paramref name="level"/>,
    /// optionally narrowed to a path through the levels above it.
    ///
    /// <para>
    /// <b><paramref name="scope"/> is what makes this a browsable report rather than a second
    /// trial balance.</b> Bound from the query string as
    /// <c>scope[0].level=1&amp;scope[0].code=1&amp;scope[1].level=2&amp;scope[1].code=10</c>, it
    /// pins the higher levels so «level=معین» means «the معین rows inside that کل». Appending a
    /// step is the way down (کل به جزء); dropping one is the way up.
    /// </para>
    ///
    /// <para>
    /// Reads the Oracle view <c>VW_CONSOLIDATE_REPORT</c> rather than the write model — the one
    /// report in the project that satisfies team working-rule #2 without the documented exception.
    /// </para>
    ///
    /// <para>
    /// Not paged, for the same reason as the trial balance: a partial aggregate is not a smaller
    /// answer, it is a wrong one. Scoped to the caller's effective unit via
    /// <c>IVahedScopedQuery</c>, hence the <b>403</b>.
    /// </para>
    /// </summary>
    [HttpGet("matrix")]
    [ProducesResponseType(typeof(MatrixReportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMatrixReport(
        [FromQuery] string year = "",
        [FromQuery] MatrixReportLevel level = MatrixReportLevel.Moin,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] string? fromVoucherNo = null,
        [FromQuery] string? toVoucherNo = null,
        [FromQuery] int? docLife = null,
        [FromQuery] Guid? systemTypeId = null,
        [FromQuery] List<MatrixReportScopeItem>? scope = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetMatrixReportQuery(
                year,
                level,
                scope,
                fromDate,
                toDate,
                fromVoucherNo,
                toVoucherNo,
                docLife,
                systemTypeId),
            cancellationToken);

        return Ok(result);
    }
}
