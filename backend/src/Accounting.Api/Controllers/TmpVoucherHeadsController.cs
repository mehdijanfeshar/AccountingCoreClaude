using Accounting.Application.Common;
using Accounting.Application.TmpVoucherHeads.Commands.CreateTmpVoucherHead;
using Accounting.Application.TmpVoucherHeads.Commands.DeleteTmpVoucherHead;
using Accounting.Application.TmpVoucherHeads.Commands.UpdateTmpVoucherHead;
using Accounting.Application.TmpVoucherHeads.Queries;
using Accounting.Application.TmpVoucherHeads.Queries.GetTmpVoucherHeadById;
using Accounting.Application.TmpVoucherHeads.Queries.GetTmpVoucherHeads;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_TMP_VOUCHERHEAD</c> (Legacy <em>temporary</em> voucher
/// header — سند موقت) write and read use cases. Every action does nothing but: build a request →
/// send it through MediatR → map the result to an <see cref="IActionResult"/>. All validation
/// lives in FluentValidation validators (run by <c>ValidationBehavior</c>) and all business rules
/// live in the Application/Domain layers — never here.
///
/// ⚠️⚠️ <b>HEAD ONLY — this is deliberate, not an oversight.</b> <c>TB_TMP_VOUCHERHEAD</c>'s
/// child table <c>TB_TMP_VOUCHERSDETAIL</c> is explicitly out of scope for this batch: the
/// aggregate boundary for this Head/Detail pair has NOT been decided (see
/// <c>docs/open-decisions.md</c>). There is no route, command, repository, or cascade touching
/// <c>TB_TMP_VOUCHERSDETAIL</c> anywhere in this batch. Do not add one without first getting the
/// aggregate-boundary decision from <c>team-lead</c>.
///
/// ⚠️⚠️ <b>For this entity specifically, Head-only is a bigger functional gap than usual, and the
/// reference project points the opposite way.</b> In <c>D:\CentralAccount</c>,
/// <c>TmpVoucherDetail</c> is <b>encapsulated</b> — there is no standalone detail command folder
/// at all, and <c>AddTmpVoucherHeadCommandHandler</c> persists head + details as a single object
/// graph (<c>docs/centralaccount-business-reference.md</c> §5). So the only sanctioned way to
/// create a temporary voucher there is compositely, whereas <b>this API can currently only ever
/// create an <em>empty</em> one</b>. That makes the promote-to-real-voucher flow
/// (<c>AddVochersImportTempCommand</c> in the reference project) unreachable from here. Closing
/// that gap needs the same explicit decision the project owner made for vouchers in phase 10
/// (composite create); it was not assumed.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update/Delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="ElamHeadsController"/>.
///
/// <b>409 Conflict is deliberately NOT declared anywhere in this controller.</b>
/// <c>TB_TMP_VOUCHERHEAD</c> has <b>no UNIQUE constraint at all</b> in <c>LegacyDbContext</c>, so
/// there is nothing for <c>UnitOfWork.SaveChangesAsync</c> to translate into a
/// <c>DuplicateKeyException</c>; declaring 409 would be speculation. ⚠️ One consequence worth
/// knowing: <c>SourceId</c> is not unique either, so the same source document can be staged
/// twice without any complaint from the database.
///
/// <b>400 also covers a mapped FK violation</b>: a <c>VoucherHeadId</c> that does not reference
/// an existing row violates <c>FK_TMP_VOCHERHEAD</c> — the table's only FK — and is mapped
/// centrally to <c>ForeignKeyViolationException</c> → 400 (plain <see cref="ProblemDetails"/>,
/// no <c>errors</c> dictionary — naming the offending field would mean leaking the Oracle
/// constraint name).
///
/// ⚠️⚠️ <b><c>SourceId</c> has NO FK at all</b> — an invalid value there is written silently and
/// the 400 above does NOT cover it. See <see cref="CreateTmpVoucherHeadCommand"/> XML doc.
/// </summary>
[ApiController]
[Route("api/tmp-voucher-heads")]
public sealed class TmpVoucherHeadsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TmpVoucherHeadsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new temporary voucher header (<c>TB_TMP_VOUCHERHEAD</c> row). HEAD ONLY — does
    /// not create any <c>TB_TMP_VOUCHERSDETAIL</c> row, so the resulting temporary voucher has
    /// no lines and no way to gain any through this API.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateTmpVoucherHeadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTmpVoucherHeadCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateTmpVoucherHeadResponse(id));
    }

    /// <summary>
    /// Returns a page of temporary voucher headers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TmpVoucherHeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTmpVoucherHeadsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single temporary voucher header by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TmpVoucherHeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTmpVoucherHeadByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing temporary voucher header (<c>TB_TMP_VOUCHERHEAD</c> row).
    /// Exposed as <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate.
    /// <c>Id</c> is taken from the route, never the body. Returns <b>200</b> with the affected
    /// <c>Id</c> in the body (not 204), mirroring every other write action in this project.
    /// HEAD ONLY — does not touch any <c>TB_TMP_VOUCHERSDETAIL</c> row.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateTmpVoucherHeadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTmpVoucherHeadRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTmpVoucherHeadCommand(
            id,
            request.VoucherHeadId,
            request.DateDoc,
            request.HeadDesc,
            request.VahedCode,
            request.Year,
            request.SysType,
            request.SourceId);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateTmpVoucherHeadResponse(id));
    }

    /// <summary>
    /// Soft-deletes a temporary voucher header (<c>TB_TMP_VOUCHERHEAD.ISDELETED = true</c>).
    /// Exposed as <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteTmpVoucherHeadCommandHandler"/> XML doc. Returns <b>200</b> with the
    /// affected <c>Id</c> in the body (not 204). HEAD ONLY — does NOT cascade to
    /// <c>TB_TMP_VOUCHERSDETAIL</c>.
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteTmpVoucherHeadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTmpVoucherHeadCommand(id), cancellationToken);

        return Ok(new DeleteTmpVoucherHeadResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="TmpVoucherHeadsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_TMP_VOUCHERHEAD.ID</c>.</param>
public sealed record CreateTmpVoucherHeadResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="TmpVoucherHeadsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_TMP_VOUCHERHEAD.ID</c> that was updated (from the route).</param>
public sealed record UpdateTmpVoucherHeadResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="TmpVoucherHeadsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_TMP_VOUCHERHEAD.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteTmpVoucherHeadResponse(Guid Id);

/// <summary>
/// Request body for <see cref="TmpVoucherHeadsController.Update"/>. Mirrors every field of
/// <see cref="UpdateTmpVoucherHeadCommand"/> except <c>Id</c>, which is bound from the route
/// instead.
/// </summary>
public sealed record UpdateTmpVoucherHeadRequest(
    Guid? VoucherHeadId,
    string? DateDoc,
    string? HeadDesc,
    string? VahedCode,
    string? Year,
    string? SysType,
    Guid? SourceId);
