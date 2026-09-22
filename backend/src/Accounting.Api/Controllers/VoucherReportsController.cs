using Accounting.Application.Reports.AccountJournal;
using Accounting.Application.Reports.AccountJournal.GetAccountJournal;
using Accounting.Application.Reports.VoucherReview;
using Accounting.Application.Reports.VoucherReview.GetVoucherReview;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Voucher-level reports — مرور اسناد and دفتر روزنامه. Routed at <c>api/reports</c> alongside
/// <see cref="TrialBalanceReportsController"/>, but kept a separate controller on purpose: these
/// two read different Oracle views, are paged where the trial balance deliberately is not, and
/// share none of its raw-SQL machinery.
///
/// <c>GET</c> only, like every read endpoint in this API — no <c>PUT</c>/<c>DELETE</c> anywhere
/// (enforced repo-wide by <c>HttpVerbConventionTests</c>). Each action does nothing but build a
/// Query from the query string, send it through MediatR, and wrap the result.
///
/// Both actions implicitly return <b>401</b> (no <c>[AllowAnonymous]</c>, so the API-wide fallback
/// policy applies) and declare <b>403</b>: both Queries implement <c>IVahedScopedQuery</c>, so
/// <c>VahedScopeBehavior</c> resolves the caller's effective unit and throws when there is none.
/// Neither action takes a <c>vahedCode</c> parameter — the unit comes from the token, optionally
/// narrowed by the <c>X-Vahed-Code</c> header against the caller's permitted subtree.
/// </summary>
[ApiController]
[Route("api/reports")]
public sealed class VoucherReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VoucherReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// مرور اسناد — a page of vouchers with each voucher's بدهکار/بستانکار totals, newest first,
    /// plus the totals of the whole filtered set.
    ///
    /// <para>
    /// What distinguishes this from the کارتابل list is the numbers: the same vouchers, each with
    /// both of its sides summed, so an out-of-balance document is visible at a glance. The سرسند
    /// view that exists for this report is not used — it double-counts lines; see
    /// <c>VoucherReviewReadRepository</c>.
    /// </para>
    /// </summary>
    /// <param name="year">سال مالی — required, 4 digits.</param>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Page size (max 200).</param>
    /// <param name="fromVoucherNo">Optional inclusive lower bound on شماره سند.</param>
    /// <param name="toVoucherNo">Optional inclusive upper bound on شماره سند.</param>
    /// <param name="fromDate">Optional Jalali <c>YYYYMMDD</c> lower bound on تاریخ سند.</param>
    /// <param name="toDate">Optional Jalali <c>YYYYMMDD</c> upper bound on تاریخ سند.</param>
    /// <param name="fromAtfNo">Optional inclusive lower bound on شماره عطف.</param>
    /// <param name="toAtfNo">Optional inclusive upper bound on شماره عطف.</param>
    /// <param name="docLife">Optional exact وضعیت سند filter (1..4).</param>
    /// <param name="systemTypeId">Optional exact نوع سند filter.</param>
    /// <param name="description">Optional «contains» filter on شرح سند.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("voucher-review")]
    [ProducesResponseType(typeof(VoucherReviewResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetVoucherReview(
        [FromQuery] string year = "",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? fromVoucherNo = null,
        [FromQuery] string? toVoucherNo = null,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] string? fromAtfNo = null,
        [FromQuery] string? toAtfNo = null,
        [FromQuery] int? docLife = null,
        [FromQuery] Guid? systemTypeId = null,
        [FromQuery] string? description = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetVoucherReviewQuery(
                pageNumber,
                pageSize,
                year,
                fromVoucherNo,
                toVoucherNo,
                fromDate,
                toDate,
                fromAtfNo,
                toAtfNo,
                docLife,
                systemTypeId,
                description),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// دفتر روزنامه — a page of posting lines in chronological order, plus the totals of the whole
    /// filtered set. Read from TB_VOUCHERSDETAIL rather than the journal view, which never excludes
    /// deleted vouchers — see <c>AccountJournalReadRepository</c>.
    /// </summary>
    /// <param name="year">سال مالی — required, 4 digits.</param>
    /// <param name="pageNumber">1-based page number.</param>
    /// <param name="pageSize">Page size (max 500 — a journal is read in long runs).</param>
    /// <param name="fromVoucherNo">Optional inclusive lower bound on شماره سند.</param>
    /// <param name="toVoucherNo">Optional inclusive upper bound on شماره سند.</param>
    /// <param name="fromDate">Optional Jalali <c>YYYYMMDD</c> lower bound on تاریخ سند.</param>
    /// <param name="toDate">Optional Jalali <c>YYYYMMDD</c> upper bound on تاریخ سند.</param>
    /// <param name="fromAccountCode">Optional inclusive lower bound on کد معین.</param>
    /// <param name="toAccountCode">Optional inclusive upper bound on کد معین.</param>
    /// <param name="docLife">Optional exact وضعیت سند filter (1..4).</param>
    /// <param name="description">Optional «contains» filter on شرح ردیف.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("account-journal")]
    [ProducesResponseType(typeof(AccountJournalResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAccountJournal(
        [FromQuery] string year = "",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? fromVoucherNo = null,
        [FromQuery] string? toVoucherNo = null,
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] string? fromAccountCode = null,
        [FromQuery] string? toAccountCode = null,
        [FromQuery] int? docLife = null,
        [FromQuery] string? description = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetAccountJournalQuery(
                pageNumber,
                pageSize,
                year,
                fromVoucherNo,
                toVoucherNo,
                fromDate,
                toDate,
                fromAccountCode,
                toAccountCode,
                docLife,
                description),
            cancellationToken);

        return Ok(result);
    }
}
