using Accounting.Application.PettyCash.Commands.ApprovePettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.BulkApprovePettyCashExpenseDocs;
using Accounting.Application.PettyCash.Commands.CreatePettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.DeletePettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.DeletePettyCashFundReviewer;
using Accounting.Application.PettyCash.Commands.RejectPettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.ReturnPettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.StartReviewPettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.SubmitPettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.UpdatePettyCashExpenseDoc;
using Accounting.Application.PettyCash.Commands.UpsertPettyCashFundReviewer;
using Accounting.Application.PettyCash.Commands.UpsertPettyCashFundSetting;
using Accounting.Application.PettyCash.Queries;
using Accounting.Application.PettyCash.Queries.GetPettyCashDocEvents;
using Accounting.Application.PettyCash.Queries.GetPettyCashExpenseDocById;
using Accounting.Application.PettyCash.Queries.GetPettyCashExpenseDocs;
using Accounting.Application.PettyCash.Queries.GetPettyCashFundReviewers;
using Accounting.Application.PettyCash.Queries.GetPettyCashFunds;
using Accounting.Application.PettyCash.Queries.GetPettyCashFundSetting;
using Accounting.Domain.ValueObjects;
using MediatR;
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
    /// Every تنخواه fund belonging to the caller's unit, with تنظیمات and computed §2 balance
    /// summary. Returns a bare array (not <see cref="Accounting.Application.Common.PagedResult{T}"/>),
    /// per the frontend contract.
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

    /// <summary>
    /// A fund's تنظیمات. Returns <b>404</b> both when the fund itself does not exist AND when it
    /// exists but has no تنظیمات row yet — the frontend treats 404 as "not configured" either way
    /// (see <see cref="GetPettyCashFundSettingQuery"/>).
    /// </summary>
    [HttpGet("funds/{fundId:guid}/settings")]
    [ProducesResponseType(typeof(PettyCashFundSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetFundSettings(Guid fundId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPettyCashFundSettingQuery(fundId), cancellationToken);

        return Ok(result);
    }

    /// <summary>Creates or fully replaces a fund's تنظیمات — «upsert».</summary>
    [HttpPost("funds/{fundId:guid}/settings")]
    [ProducesResponseType(typeof(UpsertPettyCashFundSettingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpsertFundSettings(
        Guid fundId,
        [FromBody] UpsertPettyCashFundSettingRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpsertPettyCashFundSettingCommand(
            fundId,
            request.CustodianUserId,
            request.CustodianName,
            request.PerDocLimit,
            request.AlertThresholdPercent,
            request.SettlementPeriod);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpsertPettyCashFundSettingResponse(fundId));
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

/// <summary>Request body for <see cref="PettyCashController.UpsertFundSettings"/>. <c>FundId</c> comes from the route.</summary>
public sealed record UpsertPettyCashFundSettingRequest(
    string? CustodianUserId,
    string? CustodianName,
    decimal? PerDocLimit,
    int? AlertThresholdPercent,
    PettyCashSettlementPeriod? SettlementPeriod);

/// <summary>Response body for a successful <see cref="PettyCashController.UpsertFundSettings"/> call.</summary>
public sealed record UpsertPettyCashFundSettingResponse(Guid FundId);

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
