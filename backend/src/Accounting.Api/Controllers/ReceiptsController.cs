using Accounting.Application.Common;
using Accounting.Application.Receipts.Commands.CreateReceipt;
using Accounting.Application.Receipts.Commands.DeleteReceipt;
using Accounting.Application.Receipts.Commands.UpdateReceipt;
using Accounting.Application.Receipts.Queries;
using Accounting.Application.Receipts.Queries.GetReceiptById;
using Accounting.Application.Receipts.Queries.GetReceipts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_RECEIP</c> (Legacy bank receipt/transfer document — فيش or
/// حواله) write and read use cases. Every action does nothing but: build a request → send it
/// through MediatR → map the result to an <see cref="IActionResult"/>. All validation lives in
/// FluentValidation validators (run by <c>ValidationBehavior</c>) and all business rules live in
/// the Application/Domain layers — never here.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update/Delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="ChequeTypesController"/>.
///
/// <b>409 Conflict is deliberately NOT declared on <see cref="Create"/>/<see cref="Update"/></b>
/// — like <see cref="ChequeTypesController"/>, <c>TB_RECEIP</c> carries no UNIQUE constraint at
/// all, so declaring 409 would be speculation about a constraint that does not exist.
///
/// <b>No FK exists on this table's own columns either</b>, so 400 here only ever comes from
/// FluentValidation failures — never from the central FK→400 mapping.
///
/// ⚠️ <c>ReceiptKind</c> is writable and typed as <see cref="bool"/> here, but is very likely a
/// multi-valued enum in disguise (CLAUDE.md phase 12 pattern) — see
/// <see cref="CreateReceiptCommand"/> XML doc; the CLR type is deliberately left unchanged.
///
/// ⚠️ Three other tables point AT this one (<c>TB_BANKCARTDETAIL</c>, <c>TB_PAYRECIVDETAIL</c>,
/// <c>TB_VOUCHERSDETAIL</c>). Soft-deleting a receipt via <see cref="Delete"/> does NOT cascade —
/// any referencing rows are left active and silent. See <see cref="DeleteReceiptCommand"/> XML
/// doc.
/// </summary>
[ApiController]
[Route("api/receipts")]
public sealed class ReceiptsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReceiptsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new receipt/transfer document (<c>TB_RECEIP</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateReceiptResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReceiptCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateReceiptResponse(id));
    }

    /// <summary>
    /// Returns a page of receipt/transfer documents.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReceiptDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetReceiptsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single receipt/transfer document by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReceiptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetReceiptByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing receipt/transfer document (<c>TB_RECEIP</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateReceiptRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateReceiptCommand(
            id,
            request.ReceiptKind,
            request.ReceiptDate,
            request.ReceiptNo,
            request.DateRsid,
            request.VahedCode,
            request.Year);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateReceiptResponse(id));
    }

    /// <summary>
    /// Soft-deletes a receipt/transfer document (<c>TB_RECEIP.ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteReceiptCommandHandler"/> XML doc. Returns <b>200</b> with the affected
    /// <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteReceiptCommand(id), cancellationToken);

        return Ok(new DeleteReceiptResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="ReceiptsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_RECEIP.ID</c>.</param>
public sealed record CreateReceiptResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="ReceiptsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_RECEIP.ID</c> that was updated (from the route).</param>
public sealed record UpdateReceiptResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="ReceiptsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_RECEIP.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteReceiptResponse(Guid Id);

/// <summary>
/// Request body for <see cref="ReceiptsController.Update"/>. Mirrors every field of
/// <see cref="UpdateReceiptCommand"/> except <c>Id</c>, which is bound from the route instead.
/// </summary>
public sealed record UpdateReceiptRequest(
    bool ReceiptKind,
    string ReceiptDate,
    string ReceiptNo,
    string? DateRsid,
    string VahedCode,
    string Year);
