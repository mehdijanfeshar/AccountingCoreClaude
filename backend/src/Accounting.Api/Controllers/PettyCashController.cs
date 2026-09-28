using Accounting.Application.PettyCash.Commands.ApprovePettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.BulkApprovePettyCashExpenseDocs;
using Accounting.Application.PettyCash.Commands.CreatePettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.CreatePettyCashFund;
using Accounting.Application.PettyCash.Commands.DeletePettyCashAttachment;
using Accounting.Application.PettyCash.Commands.DeletePettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.DeletePettyCashFund;
using Accounting.Application.PettyCash.Commands.DeletePettyCashFundReviewer;
using Accounting.Application.PettyCash.Commands.RejectPettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.ReturnPettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.StartReviewPettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.SubmitPettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.UpdatePettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.UpdatePettyCashFund;
using Accounting.Application.PettyCash.Commands.UploadPettyCashAttachment;
using Accounting.Application.PettyCash.Commands.UpsertPettyCashFundReviewer;
using Accounting.Application.PettyCash.Queries;
using Accounting.Application.PettyCash.Queries.GetPettyCashAttachmentFile;
using Accounting.Application.PettyCash.Queries.GetPettyCashAttachments;
using Accounting.Application.PettyCash.Queries.GetPettyCashDocEvents;
using Accounting.Application.PettyCash.Queries.GetPettyCashExpenseDocById;
using Accounting.Application.PettyCash.Queries.GetPettyCashExpenseDocs;
using Accounting.Application.PettyCash.Queries.GetPettyCashFundById;
using Accounting.Application.PettyCash.Queries.GetPettyCashFundReviewers;
using Accounting.Application.PettyCash.Queries.GetPettyCashFunds;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the petty-cash module, chunk 1 (<c>docs/tankhah-khazaneh-module.md</c>
/// §5) and chunk 2 (بخش ۲ — review/approve/return/reject/bulk-approve + reviewer RBAC CRUD,
/// same doc's "تصمیم‌های بخش ۲" section). Every action does nothing but: build a request → send
/// it through MediatR → map the
/// result to an <see cref="IActionResult"/>. No PUT/DELETE anywhere here — by explicit
/// project-owner mandate, same as every other controller: Update/Delete are
/// <c>POST {id}/update</c> and <c>POST {id}/delete</c>.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b> via the API-wide fallback
/// authorization policy.
/// </summary>
[ApiController]
[Route("api/petty-cash")]
public sealed class PettyCashController : ControllerBase
{
    private readonly IMediator _mediator;

    public PettyCashController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Every تنخواه fund belonging to the caller's unit, with computed §2 balance summary. Returns
    /// a bare array (not <see cref="Accounting.Application.Common.PagedResult{T}"/>), per the
    /// frontend contract.
    /// </summary>
    [HttpGet("funds")]
    [ProducesResponseType(typeof(IReadOnlyList<PettyCashFundDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFunds(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPettyCashFundsQuery(), cancellationToken);

        return Ok(result);
    }

    /// <summary>Returns a single تنخواه by <c>ID</c>, or 404 when it does not exist.</summary>
    [HttpGet("funds/{fundId:guid}")]
    [ProducesResponseType(typeof(PettyCashFundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFundById(Guid fundId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPettyCashFundByIdQuery(fundId), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Creates a new تنخواه (<c>TB_PC_FUND</c> row) — the module's own, fully independent تنخواه
    /// definition (2026-09-28 decision; does not touch <c>TB_REVOLVING_FUND</c>).
    /// </summary>
    [HttpPost("funds")]
    [ProducesResponseType(typeof(CreatePettyCashFundResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateFund(
        [FromBody] CreatePettyCashFundRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePettyCashFundCommand(
            request.Code,
            request.Name,
            request.CustodianUserId,
            request.CustodianName,
            request.Ceiling,
            request.PerDocLimit,
            request.AlertThresholdPercent,
            request.AccountCodeId,
            request.SettlementPeriod,
            request.IsActive);

        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetFundById), new { fundId = id }, new CreatePettyCashFundResponse(id));
    }

    /// <summary>Fully replaces an existing تنخواه. <c>fundId</c> is taken from the route, never the body.</summary>
    [HttpPost("funds/{fundId:guid}/update")]
    [ProducesResponseType(typeof(UpdatePettyCashFundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateFund(
        Guid fundId,
        [FromBody] UpdatePettyCashFundRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePettyCashFundCommand(
            fundId,
            request.Code,
            request.Name,
            request.CustodianUserId,
            request.CustodianName,
            request.Ceiling,
            request.PerDocLimit,
            request.AlertThresholdPercent,
            request.AccountCodeId,
            request.SettlementPeriod,
            request.IsActive);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdatePettyCashFundResponse(fundId));
    }

    /// <summary>
    /// Soft-deletes a تنخواه. Refused (409) while it still has non-deleted صورت‌هزینه rows.
    /// </summary>
    [HttpPost("funds/{fundId:guid}/delete")]
    [ProducesResponseType(typeof(DeletePettyCashFundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteFund(Guid fundId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePettyCashFundCommand(fundId), cancellationToken);

        return Ok(new DeletePettyCashFundResponse(fundId));
    }

    /// <summary>
    /// A page of صورت‌هزینه documents (the کارتابل) plus per-state counts. <c>states</c> is an
    /// optional comma-separated OR filter (e.g. <c>states=2,3,4</c>) that takes precedence over
    /// <c>state</c> when supplied — see <see cref="GetPettyCashExpenseDocsQuery"/>.
    /// </summary>
    [HttpGet("expense-docs")]
    [ProducesResponseType(typeof(PettyCashExpenseDocListResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetExpenseDocs(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? fundId = null,
        [FromQuery] PettyCashDocState? state = null,
        [FromQuery] string? states = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetPettyCashExpenseDocsQuery(
            pageNumber,
            pageSize,
            fundId,
            state,
            ParseStates(states),
            search);

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Parses the comma-separated <c>states</c> query parameter (e.g. <c>"2,3,4"</c>) into
    /// <see cref="PettyCashDocState"/> values. A segment that fails to parse as an integer is
    /// dropped rather than rejecting the whole request — <see cref="GetPettyCashExpenseDocsQueryValidator"/>
    /// still rejects any value that parses but is out of the enum's range.
    /// </summary>
    private static IReadOnlyList<PettyCashDocState>? ParseStates(string? states)
    {
        if (string.IsNullOrWhiteSpace(states))
        {
            return null;
        }

        var parsed = states
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(segment => int.TryParse(segment, out _))
            .Select(segment => (PettyCashDocState)int.Parse(segment))
            .ToList();

        return parsed.Count == 0 ? null : parsed;
    }

    /// <summary>Returns a single صورت‌هزینه by <c>ID</c>, or 404 when it does not exist.</summary>
    [HttpGet("expense-docs/{id:guid}")]
    [ProducesResponseType(typeof(PettyCashExpenseDocDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetExpenseDocById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPettyCashExpenseDocByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Creates a صورت‌هزینه as a پیش‌نویس, or — when <see cref="CreatePettyCashExpenseDocRequest.Submit"/>
    /// is <see langword="true"/> — submits it straight to «جدید», applying the same rules as
    /// <see cref="Submit"/>.
    /// </summary>
    [HttpPost("expense-docs")]
    [ProducesResponseType(typeof(CreatePettyCashExpenseDocResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePettyCashExpenseDocRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePettyCashExpenseDocCommand(
            request.FundId,
            request.ExpenseId,
            request.RegisterDate,
            request.Year,
            request.VendorName,
            request.VendorNationalId,
            request.InvoiceNo,
            request.InvoiceDate,
            request.EvidenceType,
            request.AmountBeforeTax,
            request.VatAmount,
            request.Description,
            request.Submit);

        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetExpenseDocById),
            new { id },
            new CreatePettyCashExpenseDocResponse(id));
    }

    /// <summary>
    /// Fully replaces an existing صورت‌هزینه. Only allowed while it is پیش‌نویس/برگشتی — see
    /// <see cref="Accounting.Application.Common.Security.PettyCashDocEditability"/>. Never
    /// changes <c>DOC_STATE</c>; use <see cref="Submit"/> for that.
    /// </summary>
    [HttpPost("expense-docs/{id:guid}/update")]
    [ProducesResponseType(typeof(UpdatePettyCashExpenseDocResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePettyCashExpenseDocRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePettyCashExpenseDocCommand(
            id,
            request.FundId,
            request.ExpenseId,
            request.RegisterDate,
            request.Year,
            request.VendorName,
            request.VendorNationalId,
            request.InvoiceNo,
            request.InvoiceDate,
            request.EvidenceType,
            request.AmountBeforeTax,
            request.VatAmount,
            request.Description);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdatePettyCashExpenseDocResponse(id));
    }

    /// <summary>Moves a صورت‌هزینه from پیش‌نویس/برگشتی to جدید. Takes no body.</summary>
    [HttpPost("expense-docs/{id:guid}/submit")]
    [ProducesResponseType(typeof(SubmitPettyCashExpenseDocResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new SubmitPettyCashExpenseDocCommand(id), cancellationToken);

        return Ok(new SubmitPettyCashExpenseDocResponse(id));
    }

    /// <summary>Soft-deletes a صورت‌هزینه. Only allowed while it is پیش‌نویس.</summary>
    [HttpPost("expense-docs/{id:guid}/delete")]
    [ProducesResponseType(typeof(DeletePettyCashExpenseDocResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePettyCashExpenseDocCommand(id), cancellationToken);

        return Ok(new DeletePettyCashExpenseDocResponse(id));
    }

    /// <summary>
    /// بخش ۲ — moves a صورت‌هزینه from جدید to «در انتظار بررسی». Only an active
    /// <c>TB_PC_REVIEWER</c> for the document's fund (who is not its own creator) may call this.
    /// Takes no body.
    /// </summary>
    [HttpPost("expense-docs/{id:guid}/start-review")]
    [ProducesResponseType(typeof(StartReviewPettyCashExpenseDocResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StartReview(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new StartReviewPettyCashExpenseDocCommand(id), cancellationToken);

        return Ok(new StartReviewPettyCashExpenseDocResponse(id));
    }

    /// <summary>بخش ۲ — moves a صورت‌هزینه from «در انتظار بررسی» to «تأییدشده».</summary>
    [HttpPost("expense-docs/{id:guid}/approve")]
    [ProducesResponseType(typeof(ApprovePettyCashExpenseDocResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] ApprovePettyCashExpenseDocRequest? request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new ApprovePettyCashExpenseDocCommand(id, request?.Note), cancellationToken);

        return Ok(new ApprovePettyCashExpenseDocResponse(id));
    }

    /// <summary>بخش ۲ — moves a صورت‌هزینه from «در انتظار بررسی» to «برگشتی»، با یک یا چند دلیل و مهلت اصلاح.</summary>
    [HttpPost("expense-docs/{id:guid}/return")]
    [ProducesResponseType(typeof(ReturnPettyCashExpenseDocResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Return(
        Guid id,
        [FromBody] ReturnPettyCashExpenseDocRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ReturnPettyCashExpenseDocCommand(id, request.ReasonCodes, request.Deadline, request.Note),
            cancellationToken);

        return Ok(new ReturnPettyCashExpenseDocResponse(id));
    }

    /// <summary>بخش ۲ — moves a صورت‌هزینه from «در انتظار بررسی» to «ردشده» (پایانی، بدون ترمیم).</summary>
    [HttpPost("expense-docs/{id:guid}/reject")]
    [ProducesResponseType(typeof(RejectPettyCashExpenseDocResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectPettyCashExpenseDocRequest? request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new RejectPettyCashExpenseDocCommand(id, request?.Note), cancellationToken);

        return Ok(new RejectPettyCashExpenseDocResponse(id));
    }

    /// <summary>
    /// بخش ۲ — approves every id in <see cref="BulkApprovePettyCashExpenseDocsRequest.Ids"/> in
    /// one all-or-nothing transaction. A 409 response's <c>failedIds</c> extension lists which ids
    /// failed and why; none of the batch is approved when any one of them fails.
    /// </summary>
    [HttpPost("expense-docs/bulk-approve")]
    [ProducesResponseType(typeof(BulkApprovePettyCashExpenseDocsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> BulkApprove(
        [FromBody] BulkApprovePettyCashExpenseDocsRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new BulkApprovePettyCashExpenseDocsCommand(request.Ids), cancellationToken);

        return Ok(new BulkApprovePettyCashExpenseDocsResponse(request.Ids));
    }

    /// <summary>بخش ۲ — every active بررسی‌کنندهٔ تنخواه configured for a fund.</summary>
    [HttpGet("funds/{fundId:guid}/reviewers")]
    [ProducesResponseType(typeof(IReadOnlyList<PettyCashFundReviewerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFundReviewers(Guid fundId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPettyCashFundReviewersQuery(fundId), cancellationToken);

        return Ok(result);
    }

    /// <summary>بخش ۲ — creates a new بررسی‌کننده for a fund, or reactivates/renames an existing one.</summary>
    [HttpPost("funds/{fundId:guid}/reviewers")]
    [ProducesResponseType(typeof(UpsertPettyCashFundReviewerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpsertFundReviewer(
        Guid fundId,
        [FromBody] UpsertPettyCashFundReviewerRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(
            new UpsertPettyCashFundReviewerCommand(fundId, request.ReviewerUserId, request.ReviewerName),
            cancellationToken);

        return Ok(new UpsertPettyCashFundReviewerResponse(id));
    }

    /// <summary>بخش ۲ — soft-deletes a بررسی‌کننده.</summary>
    [HttpPost("funds/{fundId:guid}/reviewers/{id:guid}/delete")]
    [ProducesResponseType(typeof(DeletePettyCashFundReviewerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteFundReviewer(Guid fundId, Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePettyCashFundReviewerCommand(fundId, id), cancellationToken);

        return Ok(new DeletePettyCashFundReviewerResponse(id));
    }

    /// <summary>
    /// بخش ۲-ب — every non-deleted پیوست's metadata for a صورت‌هزینه (never the file bytes). Any
    /// caller with VahedScope access to the document may call this — not owner-only.
    /// </summary>
    [HttpGet("expense-docs/{id:guid}/attachments")]
    [ProducesResponseType(typeof(IReadOnlyList<PettyCashAttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAttachments(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPettyCashAttachmentsQuery(id), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// بخش ۲-ب — uploads a پیوست. Only while the document is پیش‌نویس/برگشتی, only by the
    /// document's own owner (<see cref="Accounting.Application.Common.Interfaces.ICurrentUser.UserId"/>
    /// == <c>TB_PC_EXPENSE_DOC.ADDUSERID</c>). Reads the multipart <see cref="IFormFile"/> here and
    /// converts it to bytes before dispatching <see cref="UploadPettyCashAttachmentCommand"/> —
    /// <c>Accounting.Application</c> takes no ASP.NET Core dependency, same boundary as every
    /// other command in this project.
    /// </summary>
    [HttpPost("expense-docs/{id:guid}/attachments")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadPettyCashAttachmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadAttachment(
        Guid id,
        [FromForm] UploadPettyCashAttachmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "فایل پیوست الزامی است.");

            return ValidationProblem(ModelState);
        }

        await using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream, cancellationToken);

        var attachName = string.IsNullOrWhiteSpace(request.AttachName) ? request.File.FileName : request.AttachName!;

        var command = new UploadPettyCashAttachmentCommand(id, attachName, request.File.ContentType, stream.ToArray());

        var attachmentId = await _mediator.Send(command, cancellationToken);

        return Ok(new UploadPettyCashAttachmentResponse(attachmentId));
    }

    /// <summary>بخش ۲-ب — downloads a پیوست's bytes. The only endpoint on this controller that returns file content.</summary>
    [HttpGet("expense-docs/{id:guid}/attachments/{attachmentId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPettyCashAttachmentFileQuery(id, attachmentId), cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return File(result.Content, result.ContentType ?? "application/octet-stream", result.AttachName);
    }

    /// <summary>
    /// بخش ۲-ب — soft-deletes a پیوست. Only while the document is پیش‌نویس/برگشتی, only by the
    /// document's own owner.
    /// </summary>
    [HttpPost("expense-docs/{id:guid}/attachments/{attachmentId:guid}/delete")]
    [ProducesResponseType(typeof(DeletePettyCashAttachmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePettyCashAttachmentCommand(id, attachmentId), cancellationToken);

        return Ok(new DeletePettyCashAttachmentResponse(attachmentId));
    }

    /// <summary>«گردش عملیات» — the full audit trail for one صورت‌هزینه.</summary>
    [HttpGet("expense-docs/{id:guid}/events")]
    [ProducesResponseType(typeof(IReadOnlyList<PettyCashDocEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetEvents(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPettyCashDocEventsQuery(id), cancellationToken);

        return Ok(result);
    }
}

/// <summary>Request body for <see cref="PettyCashController.CreateFund"/>.</summary>
public sealed record CreatePettyCashFundRequest(
    string Code,
    string Name,
    string CustodianUserId,
    string? CustodianName,
    decimal Ceiling,
    decimal PerDocLimit,
    int? AlertThresholdPercent,
    Guid? AccountCodeId,
    PettyCashSettlementPeriod? SettlementPeriod,
    bool IsActive);

/// <summary>Response body for a successful <see cref="PettyCashController.CreateFund"/> call.</summary>
public sealed record CreatePettyCashFundResponse(Guid Id);

/// <summary>Request body for <see cref="PettyCashController.UpdateFund"/>. <c>FundId</c> comes from the route.</summary>
public sealed record UpdatePettyCashFundRequest(
    string Code,
    string Name,
    string CustodianUserId,
    string? CustodianName,
    decimal Ceiling,
    decimal PerDocLimit,
    int? AlertThresholdPercent,
    Guid? AccountCodeId,
    PettyCashSettlementPeriod? SettlementPeriod,
    bool IsActive);

/// <summary>Response body for a successful <see cref="PettyCashController.UpdateFund"/> call.</summary>
public sealed record UpdatePettyCashFundResponse(Guid FundId);

/// <summary>Response body for a successful <see cref="PettyCashController.DeleteFund"/> call.</summary>
public sealed record DeletePettyCashFundResponse(Guid FundId);

/// <summary>Request body for <see cref="PettyCashController.Create"/>.</summary>
public sealed record CreatePettyCashExpenseDocRequest(
    Guid FundId,
    Guid ExpenseId,
    string RegisterDate,
    string Year,
    string VendorName,
    string? VendorNationalId,
    string? InvoiceNo,
    string? InvoiceDate,
    PettyCashEvidenceType? EvidenceType,
    decimal AmountBeforeTax,
    decimal VatAmount,
    string? Description,
    bool Submit);

/// <summary>Response body for a successful <see cref="PettyCashController.Create"/> call.</summary>
public sealed record CreatePettyCashExpenseDocResponse(Guid Id);

/// <summary>Request body for <see cref="PettyCashController.Update"/>. <c>Id</c> comes from the route.</summary>
public sealed record UpdatePettyCashExpenseDocRequest(
    Guid FundId,
    Guid ExpenseId,
    string RegisterDate,
    string Year,
    string VendorName,
    string? VendorNationalId,
    string? InvoiceNo,
    string? InvoiceDate,
    PettyCashEvidenceType? EvidenceType,
    decimal AmountBeforeTax,
    decimal VatAmount,
    string? Description);

/// <summary>Response body for a successful <see cref="PettyCashController.Update"/> call.</summary>
public sealed record UpdatePettyCashExpenseDocResponse(Guid Id);

/// <summary>Response body for a successful <see cref="PettyCashController.Submit"/> call.</summary>
public sealed record SubmitPettyCashExpenseDocResponse(Guid Id);

/// <summary>Response body for a successful <see cref="PettyCashController.Delete"/> call.</summary>
public sealed record DeletePettyCashExpenseDocResponse(Guid Id);

/// <summary>Response body for a successful <see cref="PettyCashController.StartReview"/> call.</summary>
public sealed record StartReviewPettyCashExpenseDocResponse(Guid Id);

/// <summary>Request body for <see cref="PettyCashController.Approve"/>. May be omitted entirely (no note).</summary>
public sealed record ApprovePettyCashExpenseDocRequest(string? Note);

/// <summary>Response body for a successful <see cref="PettyCashController.Approve"/> call.</summary>
public sealed record ApprovePettyCashExpenseDocResponse(Guid Id);

/// <summary>Request body for <see cref="PettyCashController.Return"/>.</summary>
public sealed record ReturnPettyCashExpenseDocRequest(
    IReadOnlyList<int> ReasonCodes,
    string Deadline,
    string? Note);

/// <summary>Response body for a successful <see cref="PettyCashController.Return"/> call.</summary>
public sealed record ReturnPettyCashExpenseDocResponse(Guid Id);

/// <summary>Request body for <see cref="PettyCashController.Reject"/>. May be omitted entirely (no note).</summary>
public sealed record RejectPettyCashExpenseDocRequest(string? Note);

/// <summary>Response body for a successful <see cref="PettyCashController.Reject"/> call.</summary>
public sealed record RejectPettyCashExpenseDocResponse(Guid Id);

/// <summary>Request body for <see cref="PettyCashController.BulkApprove"/>.</summary>
public sealed record BulkApprovePettyCashExpenseDocsRequest(IReadOnlyList<Guid> Ids);

/// <summary>Response body for a successful (all ids approved) <see cref="PettyCashController.BulkApprove"/> call.</summary>
public sealed record BulkApprovePettyCashExpenseDocsResponse(IReadOnlyList<Guid> Ids);

/// <summary>Request body for <see cref="PettyCashController.UpsertFundReviewer"/>. <c>FundId</c> comes from the route.</summary>
public sealed record UpsertPettyCashFundReviewerRequest(string ReviewerUserId, string? ReviewerName);

/// <summary>Response body for a successful <see cref="PettyCashController.UpsertFundReviewer"/> call.</summary>
public sealed record UpsertPettyCashFundReviewerResponse(Guid Id);

/// <summary>Response body for a successful <see cref="PettyCashController.DeleteFundReviewer"/> call.</summary>
public sealed record DeletePettyCashFundReviewerResponse(Guid Id);

/// <summary>
/// Multipart/form-data request body for <see cref="PettyCashController.UploadAttachment"/>. A
/// plain class rather than a positional record — ASP.NET Core's form-value binder for
/// <see cref="IFormFile"/> properties is most reliably exercised against settable properties, and
/// no other command in this project's API binds from a multipart form, so there is no existing
/// precedent to follow here. <see cref="AttachName"/> is optional; the controller falls back to
/// the uploaded file's own name when omitted.
/// </summary>
public sealed class UploadPettyCashAttachmentRequest
{
    public IFormFile File { get; set; } = null!;

    public string? AttachName { get; set; }
}

/// <summary>Response body for a successful <see cref="PettyCashController.UploadAttachment"/> call.</summary>
public sealed record UploadPettyCashAttachmentResponse(Guid Id);

/// <summary>Response body for a successful <see cref="PettyCashController.DeleteAttachment"/> call.</summary>
public sealed record DeletePettyCashAttachmentResponse(Guid Id);
