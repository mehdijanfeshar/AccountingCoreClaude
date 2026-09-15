using Accounting.Application.Common;
using Accounting.Application.ElamHeads.Commands.CreateElamHead;
using Accounting.Application.ElamHeads.Commands.DeleteElamHead;
using Accounting.Application.ElamHeads.Commands.UpdateElamHead;
using Accounting.Application.ElamHeads.Queries;
using Accounting.Application.ElamHeads.Queries.GetElamHeadById;
using Accounting.Application.ElamHeads.Queries.GetElamHeads;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_ELAMHEAD</c> (Legacy announcement/notice header — اعلاميه)
/// write and read use cases. Every action does nothing but: build a request → send it through
/// MediatR → map the result to an <see cref="IActionResult"/>. All validation lives in
/// FluentValidation validators (run by <c>ValidationBehavior</c>) and all business rules live
/// in the Application/Domain layers — never here.
///
/// ⚠️⚠️ <b>HEAD ONLY — this is deliberate, not an oversight.</b> <c>TB_ELAMHEAD</c>'s child
/// table <c>TB_ELAMDETAIL</c> is explicitly out of scope for this batch: the aggregate boundary
/// for this Head/Detail pair has NOT been decided (see <c>docs/open-decisions.md</c>). This
/// controller follows the phase-5..8 <c>VoucherHead</c> precedent from before the phase-10
/// "composite aggregate" decision was made for vouchers — standalone Head CRUD only. There is
/// no route, command, repository, or cascade touching <c>TB_ELAMDETAIL</c> anywhere in this
/// batch. Do not add one without first getting the aggregate-boundary decision from
/// <c>team-lead</c>.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update/Delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="WorkShopsController"/>.
///
/// <b><see cref="Create"/>/<see cref="Update"/>/<see cref="GetList"/> also declare <c>403
/// Forbidden</c></b> — <c>CreateElamHeadCommand</c>/<c>UpdateElamHeadCommand</c>/
/// <c>GetElamHeadsQuery</c> all implement <c>IVahedScopedCommand</c>/<c>IVahedScopedQuery</c>, so
/// <c>VahedScopeBehavior</c> throws <c>MissingVahedScopeException</c> → 403 (via
/// <c>GlobalExceptionHandler</c>) when the authenticated caller has no usable unit-scope claim.
/// <see cref="GetById"/>/<see cref="Delete"/> do not opt in and never return 403 for this reason.
///
/// <b>409 Conflict IS declared on <see cref="Create"/>/<see cref="Update"/></b> —
/// <c>TB_ELAMHEAD</c> carries a real UNIQUE constraint (<c>AK_AK_ELAMHEAD_ELAMHEAD</c> on
/// <c>ELAMH_SERIALNO, ELAMH_CODE, VAHEDCODE</c>), mapped centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> to <c>DuplicateKeyException</c> → 409.
///
/// <b>400 also covers a mapped FK violation</b>: a <c>VoucherHeadId</c> that does not reference
/// an existing row violates <c>FK_ELAM_VOUCHER</c> and is mapped centrally to
/// <c>ForeignKeyViolationException</c> → 400 (plain <see cref="ProblemDetails"/>, no
/// <c>errors</c> dictionary — naming the offending field would mean leaking the Oracle
/// constraint name).
///
/// ⚠️⚠️ <b><c>WorkShopId</c> and <c>ElamSenderId</c> have NO FK at all</b> — invalid values on
/// either are written silently; the 400 above does NOT cover them. See
/// <see cref="CreateElamHeadCommand"/> XML doc.
///
/// ⚠️⚠️ <b>Two confirmed <c>bool?</c>-should-be-enum columns, flagged not fixed</b>:
/// <c>Case</c> (<c>ELAMH_CASE</c> — real values 1/2, comment «نوع اعلاميه 1بد 2بس») and
/// <c>DramadType</c> (<c>ELAMHDRAMAD_TYPE</c> — real values 1/2/3, comment
/// « 3حق بيمه نوع اعلاميه 1ذي حسابي 2سايردرآمد», the third value unreachable through
/// <see cref="bool"/>?). See <see cref="CreateElamHeadCommand"/> XML doc for the full
/// phase-12-pattern write-up. Re-typing either is a breaking API-contract change and out of
/// scope for this batch.
/// </summary>
[ApiController]
[Route("api/elam-heads")]
public sealed class ElamHeadsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ElamHeadsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new announcement header (<c>TB_ELAMHEAD</c> row). HEAD ONLY — does not create
    /// any <c>TB_ELAMDETAIL</c> row.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateElamHeadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateElamHeadCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateElamHeadResponse(id));
    }

    /// <summary>
    /// Returns a page of announcement headers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ElamHeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetElamHeadsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single announcement header by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ElamHeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetElamHeadByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing announcement header (<c>TB_ELAMHEAD</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project. HEAD ONLY — does
    /// not touch any <c>TB_ELAMDETAIL</c> row.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateElamHeadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateElamHeadRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateElamHeadCommand(
            id,
            request.VoucherHeadId,
            request.SerialNo,
            request.Code,
            request.DabirNo,
            request.DabirDate,
            request.PrintNo,
            request.Case,
            request.SerialNoInput,
            request.WebStat,
            request.Date,
            request.Desc,
            request.WorkShopId,
            request.RcvNo,
            request.RcvDt,
            request.LstMon,
            request.PayNo,
            request.DramadType,
            request.PeimanNo,
            request.WorkShopCode,
            request.WorkShopName,
            request.SendRcvVahed,
            request.ElamYear,
            request.Year,
            request.ElamSenderId);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateElamHeadResponse(id));
    }

    /// <summary>
    /// Soft-deletes an announcement header (<c>TB_ELAMHEAD.ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteElamHeadCommandHandler"/> XML doc. Returns <b>200</b> with the affected
    /// <c>Id</c> in the body (not 204). HEAD ONLY — does NOT cascade to <c>TB_ELAMDETAIL</c>.
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteElamHeadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteElamHeadCommand(id), cancellationToken);

        return Ok(new DeleteElamHeadResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="ElamHeadsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_ELAMHEAD.ID</c>.</param>
public sealed record CreateElamHeadResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="ElamHeadsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_ELAMHEAD.ID</c> that was updated (from the route).</param>
public sealed record UpdateElamHeadResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="ElamHeadsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_ELAMHEAD.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteElamHeadResponse(Guid Id);

/// <summary>
/// Request body for <see cref="ElamHeadsController.Update"/>. Mirrors every field of
/// <see cref="UpdateElamHeadCommand"/> except <c>Id</c> (bound from the route instead) and
/// <c>VahedCode</c> (server-assigned by <c>VahedScopeBehavior</c> — see
/// <see cref="UpdateElamHeadCommand.VahedCode"/> XML doc — so it is not part of this request
/// body at all, not even as an ignored field).
/// </summary>
public sealed record UpdateElamHeadRequest(
    Guid? VoucherHeadId,
    string? SerialNo,
    string? Code,
    string? DabirNo,
    string? DabirDate,
    short? PrintNo,
    bool? Case,
    string? SerialNoInput,
    byte? WebStat,
    string? Date,
    string? Desc,
    Guid? WorkShopId,
    string? RcvNo,
    string? RcvDt,
    string? LstMon,
    string? PayNo,
    bool? DramadType,
    string? PeimanNo,
    string? WorkShopCode,
    string? WorkShopName,
    string? SendRcvVahed,
    string? ElamYear,
    string? Year,
    Guid? ElamSenderId);
