using Accounting.Application.Common;
using Accounting.Application.WorkShops.Commands.CreateWorkShop;
using Accounting.Application.WorkShops.Commands.DeleteWorkShop;
using Accounting.Application.WorkShops.Commands.UpdateWorkShop;
using Accounting.Application.WorkShops.Queries;
using Accounting.Application.WorkShops.Queries.GetWorkShopById;
using Accounting.Application.WorkShops.Queries.GetWorkShops;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_WORKSHOP</c> (Legacy workshop/production-line master) write
/// and read use cases. Every action does nothing but: build a request → send it through
/// MediatR → map the result to an <see cref="IActionResult"/>. All validation lives in
/// FluentValidation validators (run by <c>ValidationBehavior</c>) and all business rules live
/// in the Application/Domain layers — never here.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update/Delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="RabetsController"/>.
///
/// <b>409 Conflict IS declared on <see cref="Create"/>/<see cref="Update"/></b> —
/// <c>TB_WORKSHOP</c> carries a real UNIQUE constraint (<c>UK_WORKSHOP</c> on
/// <c>WORKSHOPCODE, ISACTIVE, VAHEDCODE</c>), mapped centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> to <c>DuplicateKeyException</c> → 409.
///
/// <b>400 also covers both mapped FK violations</b>: an <c>AccountCodeId</c>/<c>BranchId</c>
/// that does not reference an existing row violates <c>FK_WORK_ACCOUNTCODE</c>/
/// <c>FK_WORK_VAHEDINFO</c> and is mapped centrally to <c>ForeignKeyViolationException</c> →
/// 400 (plain <see cref="ProblemDetails"/>, no <c>errors</c> dictionary — naming the offending
/// field would mean leaking the Oracle constraint name).
///
/// ⚠️ <c>ISACTIVE</c> is writable via Create/Update and is a plain <see cref="bool"/> here —
/// see <c>CreateWorkShopCommand</c> XML doc for the unverified-enum flag (CLAUDE.md phase 12
/// pattern). ⚠️ <c>CheckFile</c> (BLOB) has no size limit enforced at this layer — an
/// upload/request-size policy is an unmade decision.
/// </summary>
[ApiController]
[Route("api/work-shops")]
public sealed class WorkShopsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkShopsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new workshop (<c>TB_WORKSHOP</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateWorkShopResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWorkShopCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateWorkShopResponse(id));
    }

    /// <summary>
    /// Returns a page of workshops.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WorkShopDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetWorkShopsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single workshop by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WorkShopDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWorkShopByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing workshop (<c>TB_WORKSHOP</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateWorkShopResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateWorkShopRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWorkShopCommand(
            id,
            request.AccountCodeId,
            request.BranchId,
            request.WorkShopName,
            request.WorkShopCode,
            request.VahedCode,
            request.IsActive,
            request.CheckFile);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateWorkShopResponse(id));
    }

    /// <summary>
    /// Soft-deletes a workshop (<c>TB_WORKSHOP.ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteWorkShopCommandHandler"/> XML doc. Returns <b>200</b> with the affected
    /// <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteWorkShopResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteWorkShopCommand(id), cancellationToken);

        return Ok(new DeleteWorkShopResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="WorkShopsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_WORKSHOP.ID</c>.</param>
public sealed record CreateWorkShopResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="WorkShopsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_WORKSHOP.ID</c> that was updated (from the route).</param>
public sealed record UpdateWorkShopResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="WorkShopsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_WORKSHOP.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteWorkShopResponse(Guid Id);

/// <summary>
/// Request body for <see cref="WorkShopsController.Update"/>. Mirrors every field of
/// <see cref="UpdateWorkShopCommand"/> except <c>Id</c>, which is bound from the route instead.
/// </summary>
public sealed record UpdateWorkShopRequest(
    Guid AccountCodeId,
    Guid? BranchId,
    string WorkShopName,
    string WorkShopCode,
    string VahedCode,
    bool IsActive,
    byte[]? CheckFile);
