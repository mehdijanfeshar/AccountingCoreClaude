using Accounting.Application.Common;
using Accounting.Application.Vouchers.Commands.CreateVoucherHead;
using Accounting.Application.Vouchers.Commands.DeleteVoucherHead;
using Accounting.Application.Vouchers.Commands.UpdateVoucherHead;
using Accounting.Application.Vouchers.Queries;
using Accounting.Application.Vouchers.Queries.GetVoucherHeadById;
using Accounting.Application.Vouchers.Queries.GetVoucherHeads;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_VOUCHERSHEAD</c> (Legacy voucher header) write and read
/// use cases. Named "voucher heads" (not "vouchers") to match the underlying resource, so
/// voucher lines can later be exposed at <c>/api/voucher-heads/{id}/lines</c> without a rename.
/// Every action does nothing but: build a request → send it through MediatR → map the result
/// to an <see cref="IActionResult"/>.
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
/// <c>{id}/update</c> and <c>{id}/delete</c> instead. See the XML doc on <see cref="Update"/>
/// and <see cref="Delete"/> for the exact route shape and response contract.
/// </summary>
[ApiController]
[Route("api/voucher-heads")]
public sealed class VoucherHeadsController : ControllerBase
{
    private readonly IMediator _mediator;

    public VoucherHeadsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new voucher head (<c>TB_VOUCHERSHEAD</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateVoucherHeadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateVoucherHeadCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateVoucherHeadResponse(id));
    }

    /// <summary>
    /// Returns a page of voucher heads, optionally filtered by <c>Year</c>/<c>VahedCode</c>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<VoucherHeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? year = null,
        [FromQuery] string? vahedCode = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetVoucherHeadsQuery(pageNumber, pageSize, year, vahedCode),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single voucher head by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(VoucherHeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVoucherHeadByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing voucher head (<c>TB_VOUCHERSHEAD</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate, <c>PUT</c>
    /// and <c>DELETE</c> are not usable in this environment, so this action deliberately deviates
    /// from REST verb semantics. Not <c>POST /api/voucher-heads/{id}</c> (without a trailing
    /// segment) either, because that would be ambiguous with "create a sub-resource under this
    /// id" now that <c>POST /api/voucher-heads</c> (create) already exists. See
    /// <see cref="UpdateVoucherHeadCommand"/> XML doc for the field-level replace-vs-patch
    /// rationale. <c>Id</c> is taken from the route, never the body, so there is no possibility
    /// of a route/body id mismatch. Returns <b>200</b> with the affected <c>Id</c> in the body
    /// (not 204) so every write action on this controller has a uniform response shape.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateVoucherHeadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVoucherHeadRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateVoucherHeadCommand(
            id,
            request.DocNum,
            request.DateDoc,
            request.DocLife,
            request.HeadDesc,
            request.Apendix,
            request.SystemTypeId,
            request.FlagState,
            request.VahedCode,
            request.Year,
            request.IsAutomatic,
            request.SndVahedCode,
            request.ParentHeadId,
            request.AttachFileName,
            request.AtfNum);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateVoucherHeadResponse(id));
    }

    /// <summary>
    /// Soft-deletes a voucher head (<c>TB_VOUCHERSHEAD.ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate, <c>PUT</c>
    /// and <c>DELETE</c> are not usable in this environment, so this action deliberately deviates
    /// from REST verb semantics. Idempotent: a row that is already soft-deleted still returns
    /// 200, matching HTTP DELETE semantics — see <see cref="DeleteVoucherHeadCommandHandler"/>
    /// XML doc. Returns <b>200</b> with the affected <c>Id</c> in the body (not 204) so every
    /// write action on this controller has a uniform response shape.
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteVoucherHeadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteVoucherHeadCommand(id), cancellationToken);

        return Ok(new DeleteVoucherHeadResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="VoucherHeadsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_VOUCHERSHEAD.ID</c>.</param>
public sealed record CreateVoucherHeadResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="VoucherHeadsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_VOUCHERSHEAD.ID</c> that was updated (from the route).</param>
public sealed record UpdateVoucherHeadResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="VoucherHeadsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_VOUCHERSHEAD.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteVoucherHeadResponse(Guid Id);

/// <summary>
/// Request body for <see cref="VoucherHeadsController.Update"/>. Mirrors every field of
/// <see cref="UpdateVoucherHeadCommand"/> except <c>Id</c>, which is bound from the route
/// instead — this deliberately prevents a route id/body id mismatch from ever reaching the
/// handler.
/// </summary>
public sealed record UpdateVoucherHeadRequest(
    string DocNum,
    string DateDoc,
    bool? DocLife,
    string? HeadDesc,
    string? Apendix,
    Guid? SystemTypeId,
    decimal? FlagState,
    string VahedCode,
    string Year,
    bool? IsAutomatic,
    string? SndVahedCode,
    Guid? ParentHeadId,
    string? AttachFileName,
    string? AtfNum);
