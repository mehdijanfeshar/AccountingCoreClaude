using Accounting.Application.Common;
using Accounting.Application.PersonActions.Commands.CreatePersonAction;
using Accounting.Application.PersonActions.Commands.DeletePersonAction;
using Accounting.Application.PersonActions.Commands.UpdatePersonAction;
using Accounting.Application.PersonActions.Queries;
using Accounting.Application.PersonActions.Queries.GetPersonActionById;
using Accounting.Application.PersonActions.Queries.GetPersonActions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_PERSON_ACTION</c> (Legacy per-user action/session grant
/// window) write and read use cases. Every action does nothing but: build a request → send it
/// through MediatR → map the result to an <see cref="IActionResult"/>.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b> via the API-wide fallback
/// policy (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate</b>, exactly
/// as in <see cref="AccountCodesController"/>: update/delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>.
///
/// <b>409 Conflict IS declared on <see cref="Create"/>/<see cref="Update"/></b> — unlike
/// <see cref="AccountCodeInterfacesController"/>, <see cref="AccountExceptionsController"/> and
/// <see cref="BillLogsController"/>, <c>TB_PERSON_ACTION</c> carries a real UNIQUE constraint
/// (<c>UK_PERSON_ACTION</c> on <c>USERID</c>/<c>FROMDATE</c>/<c>TODATE</c>), so a duplicate
/// combination surfaces as a genuine 409 via the central ORA-00001 →
/// <c>DuplicateKeyException</c> mapping in <c>UnitOfWork</c>, exactly like
/// <see cref="AccountCodesController.Create"/>. No pre-check is performed — the DB constraint is
/// the single source of truth, which also handles race conditions correctly.
/// </summary>
[ApiController]
[Route("api/person-actions")]
public sealed class PersonActionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PersonActionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new person action grant window (<c>TB_PERSON_ACTION</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePersonActionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePersonActionCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreatePersonActionResponse(id));
    }

    /// <summary>
    /// Returns a page of person action grant windows.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PersonActionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPersonActionsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single person action grant window by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PersonActionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPersonActionByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing person action grant window. Exposed as <c>POST {id}/update</c>,
    /// not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is taken from the route,
    /// never the body. Returns <b>200</b> with the affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdatePersonActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePersonActionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePersonActionCommand(
            id,
            request.UserName,
            request.UserId,
            request.FromDate,
            request.ToDate,
            request.Status,
            request.OperatorRole,
            request.VahedCode);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdatePersonActionResponse(id));
    }

    /// <summary>
    /// Soft-deletes a person action grant window (<c>ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200. Returns <b>200</b>
    /// with the affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeletePersonActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePersonActionCommand(id), cancellationToken);

        return Ok(new DeletePersonActionResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="PersonActionsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_PERSON_ACTION.ID</c>.</param>
public sealed record CreatePersonActionResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="PersonActionsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_PERSON_ACTION.ID</c> that was updated (from the route).</param>
public sealed record UpdatePersonActionResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="PersonActionsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_PERSON_ACTION.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeletePersonActionResponse(Guid Id);

/// <summary>
/// Request body for <see cref="PersonActionsController.Update"/>. Mirrors every field of
/// <see cref="UpdatePersonActionCommand"/> except <c>Id</c>, which is bound from the route
/// instead.
/// </summary>
public sealed record UpdatePersonActionRequest(
    string? UserName,
    string UserId,
    string? FromDate,
    string? ToDate,
    bool? Status,
    bool OperatorRole,
    string? VahedCode);
