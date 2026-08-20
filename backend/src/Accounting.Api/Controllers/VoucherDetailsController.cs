using Accounting.Application.Common;
using Accounting.Application.Vouchers.Commands.CreateVoucherDetail;
using Accounting.Application.Vouchers.Commands.DeleteVoucherDetail;
using Accounting.Application.Vouchers.Commands.UpdateVoucherDetail;
using Accounting.Application.Vouchers.Queries;
using Accounting.Application.Vouchers.Queries.GetVoucherDetailById;
using Accounting.Application.Vouchers.Queries.GetVoucherDetails;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_VOUCHERSDETAIL</c> (Legacy voucher detail line) write and
/// read use cases. Routed at <c>api/voucher-details</c> — deliberately NOT nested under
/// <c>api/voucher-heads/{id}/lines</c> — because the project owner declared
/// <c>TB_VOUCHERSDETAIL</c> an independent aggregate root (2026-08-20 decision, recorded in
/// <c>docs/tamin-core-entity-reference.md</c> بخش ۵), and every other Legacy entity in this
/// project already gets its own top-level controller (<see cref="AccountCodesController"/>,
/// <see cref="VoucherHeadsController"/>). Creating a voucher head TOGETHER WITH its opening
/// detail lines in one atomic call is still possible — see
/// <c>CreateVoucherHeadCommand.InitialDetails</c> on <see cref="VoucherHeadsController.Create"/>
/// — but that is a composite write on the head resource, not a reason to nest this controller
/// under it. Every action does nothing but: build a request → send it through MediatR → map the
/// result to an <see cref="IActionResult"/>.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>). That response is
/// produced by the authentication middleware — before MVC/MediatR ever run — not by
/// <c>GlobalExceptionHandler</c>, but it is still documented via <c>[ProducesResponseType]</c>
/// on every action for an accurate, uniform OpenAPI contract.
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> The network infrastructure this API runs behind does not
/// allow <c>PUT</c>/<c>DELETE</c> verbs, so update/delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c> instead, mirroring <see cref="AccountCodesController"/>
/// and <see cref="VoucherHeadsController"/> exactly. See the XML doc on <see cref="Update"/> and
/// <see cref="Delete"/> for the exact route shape and response contract.
///
/// <b>409 Conflict is deliberately NOT declared on <see cref="Create"/></b> — unlike
/// <see cref="AccountCodesController.Create"/> and <see cref="VoucherHeadsController.Create"/>,
/// which both declare it because their tables carry a real UNIQUE constraint
/// (<c>UK_ACCOUNTCODE</c>, <c>UK_VOUCHERHEAD_NUMBER</c>). <c>TB_VOUCHERSDETAIL</c> has NO
/// UNIQUE constraint at all — only the non-unique indexes <c>IDX_VDETAIL_ACC_VHEAD_ISDEL</c>,
/// <c>IDX_VOUCHERSDETAIL_HEADID</c> and <c>IDX_YEAR_VAHED</c> — so declaring 409 here would be
/// speculation about a constraint that does not exist. <see cref="Create"/> DOES declare
/// <b>404</b> instead — a genuine contract difference from the other two controllers' create
/// actions — because creating a detail line requires an already-existing parent voucher head;
/// see <see cref="CreateVoucherDetailCommandHandler"/> XML doc.
/// </summary>
[ApiController]
[Route("api/voucher-details")]
public sealed class VoucherDetailsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VoucherDetailsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new voucher detail line (<c>TB_VOUCHERSDETAIL</c> row) on an already-existing
    /// voucher head. Returns 404 when <c>VoucherHeadId</c> does not reference an existing,
    /// non-soft-deleted <c>TB_VOUCHERSHEAD</c> row.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateVoucherDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateVoucherDetailCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateVoucherDetailResponse(id));
    }

    /// <summary>
    /// Returns a page of voucher detail lines, optionally filtered by
    /// <c>voucherHeadId</c>/<c>year</c>/<c>vahedCode</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VoucherDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? voucherHeadId = null,
        [FromQuery] string? year = null,
        [FromQuery] string? vahedCode = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetVoucherDetailsQuery(pageNumber, pageSize, voucherHeadId, year, vahedCode),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single voucher detail line by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VoucherDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVoucherDetailByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing voucher detail line (<c>TB_VOUCHERSDETAIL</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate, <c>PUT</c>
    /// and <c>DELETE</c> are not usable in this environment. Not
    /// <c>POST /api/voucher-details/{id}</c> (without a trailing segment) either, because that
    /// would be ambiguous with "create a sub-resource under this id" now that
    /// <c>POST /api/voucher-details</c> (create) already exists. See
    /// <see cref="UpdateVoucherDetailCommand"/> XML doc for the field-level replace-vs-patch
    /// rationale, including why <c>VoucherHeadId</c> is not part of the request body (reparenting
    /// is not supported). <c>Id</c> is taken from the route, never the body, so there is no
    /// possibility of a route/body id mismatch. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204) so every write action on this controller has a uniform response shape.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateVoucherDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVoucherDetailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateVoucherDetailCommand(
            id,
            request.AccountId,
            request.ReceiptId,
            request.CheckId,
            request.LowLevelCodeId,
            request.EtebarId,
            request.Description,
            request.Radif,
            request.Debtor,
            request.Creditor,
            request.VahedCode,
            request.Year);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateVoucherDetailResponse(id));
    }

    /// <summary>
    /// Soft-deletes a voucher detail line (<c>TB_VOUCHERSDETAIL.ISDELETED = true</c>), cascading
    /// to its own tafsili links. Exposed as <c>POST {id}/delete</c>, not <c>DELETE</c> — by
    /// explicit project-owner mandate. Idempotent: a row that is already soft-deleted still
    /// returns 200, matching HTTP DELETE semantics — see
    /// <see cref="DeleteVoucherDetailCommandHandler"/> XML doc. Returns <b>200</b> with the
    /// affected <c>Id</c> in the body (not 204) so every write action on this controller has a
    /// uniform response shape.
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteVoucherDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteVoucherDetailCommand(id), cancellationToken);

        return Ok(new DeleteVoucherDetailResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="VoucherDetailsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_VOUCHERSDETAIL.ID</c>.</param>
public sealed record CreateVoucherDetailResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="VoucherDetailsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_VOUCHERSDETAIL.ID</c> that was updated (from the route).</param>
public sealed record UpdateVoucherDetailResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="VoucherDetailsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_VOUCHERSDETAIL.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteVoucherDetailResponse(Guid Id);

/// <summary>
/// Request body for <see cref="VoucherDetailsController.Update"/>. Mirrors every field of
/// <see cref="UpdateVoucherDetailCommand"/> except <c>Id</c> (bound from the route instead) and
/// except <c>VoucherHeadId</c>, which is not updatable at all — see
/// <see cref="UpdateVoucherDetailCommand"/> XML doc for the "no reparenting" rationale.
/// </summary>
public sealed record UpdateVoucherDetailRequest(
    Guid? AccountId,
    Guid? ReceiptId,
    Guid? CheckId,
    Guid? LowLevelCodeId,
    Guid? EtebarId,
    string? Description,
    int? Radif,
    decimal? Debtor,
    decimal? Creditor,
    string? VahedCode,
    string? Year);
