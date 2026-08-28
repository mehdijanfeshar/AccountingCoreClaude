using Accounting.Application.Common;
using Accounting.Application.TafsilGroups.Commands.CreateTafsilGroup;
using Accounting.Application.TafsilGroups.Commands.DeleteTafsilGroup;
using Accounting.Application.TafsilGroups.Commands.UpdateTafsilGroup;
using Accounting.Application.TafsilGroups.Queries;
using Accounting.Application.TafsilGroups.Queries.GetTafsilGroupById;
using Accounting.Application.TafsilGroups.Queries.GetTafsilGroups;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_TAFSIL_GROUP</c> (Legacy tafsili-group lookup) write and
/// read use cases. Every action does nothing but: build a request → send it through MediatR →
/// map the result to an <see cref="IActionResult"/>. All validation lives in FluentValidation
/// validators (run by <c>ValidationBehavior</c>) and all business rules live in the
/// Application/Domain layers — never here.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update/Delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="AccountCodesController"/>.
///
/// <b>409 Conflict IS declared on <see cref="Create"/>/<see cref="Update"/></b> — <c>TB_TAFSIL_GROUP</c>
/// carries a real UNIQUE constraint (<c>UK_TBTAFSILGROUP</c> on
/// <c>(TAFSILGROUP_CODE, ISDELETED)</c>), mapped centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> to <c>DuplicateKeyException</c> → 409. No pre-check is
/// performed — the DB constraint is the single source of truth, which also handles race
/// conditions correctly. Note that <c>ISDELETED</c> itself participates in this key — see
/// <see cref="CreateTafsilGroupCommand"/> XML doc for the observed implication.
///
/// <b>400 does NOT cover FK violations</b> — <c>TB_TAFSIL_GROUP</c> has no FK of its own, so 400
/// here is FluentValidation failures plus the 409-adjacent uniqueness path above; no
/// <c>ForeignKeyViolationException</c> can ever originate from a write to this table.
/// </summary>
[ApiController]
[Route("api/tafsil-groups")]
public sealed class TafsilGroupsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TafsilGroupsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new tafsili-group lookup entry (<c>TB_TAFSIL_GROUP</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateTafsilGroupResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTafsilGroupCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateTafsilGroupResponse(id));
    }

    /// <summary>
    /// Returns a page of tafsili-group lookup entries.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TafsilGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTafsilGroupsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single tafsili-group lookup entry by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TafsilGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTafsilGroupByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing tafsili-group lookup entry (<c>TB_TAFSIL_GROUP</c> row).
    /// Exposed as <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate.
    /// <c>Id</c> is taken from the route, never the body. Returns <b>200</b> with the affected
    /// <c>Id</c> in the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateTafsilGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTafsilGroupRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTafsilGroupCommand(
            id,
            request.TafsilGroupCode,
            request.TafsilGroupName,
            request.PersonType);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateTafsilGroupResponse(id));
    }

    /// <summary>
    /// Soft-deletes a tafsili-group lookup entry (<c>TB_TAFSIL_GROUP.ISDELETED = true</c>).
    /// Exposed as <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteTafsilGroupCommandHandler"/> XML doc. Returns <b>200</b> with the
    /// affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteTafsilGroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTafsilGroupCommand(id), cancellationToken);

        return Ok(new DeleteTafsilGroupResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="TafsilGroupsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_TAFSIL_GROUP.ID</c>.</param>
public sealed record CreateTafsilGroupResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="TafsilGroupsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_TAFSIL_GROUP.ID</c> that was updated (from the route).</param>
public sealed record UpdateTafsilGroupResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="TafsilGroupsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_TAFSIL_GROUP.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteTafsilGroupResponse(Guid Id);

/// <summary>
/// Request body for <see cref="TafsilGroupsController.Update"/>. Mirrors every field of
/// <see cref="UpdateTafsilGroupCommand"/> except <c>Id</c>, which is bound from the route
/// instead.
/// </summary>
public sealed record UpdateTafsilGroupRequest(
    string TafsilGroupCode,
    string TafsilGroupName,
    bool? PersonType);
