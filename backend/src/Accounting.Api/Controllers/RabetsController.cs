using Accounting.Application.Common;
using Accounting.Application.Rabets.Commands.CreateRabet;
using Accounting.Application.Rabets.Commands.DeleteRabet;
using Accounting.Application.Rabets.Commands.UpdateRabet;
using Accounting.Application.Rabets.Queries;
using Accounting.Application.Rabets.Queries.GetRabetById;
using Accounting.Application.Rabets.Queries.GetRabets;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_RABET</c> (Legacy account-code/rabet-type link) write and
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
/// <b>409 Conflict IS declared on <see cref="Create"/>/<see cref="Update"/></b> — unlike
/// <see cref="PreDescribsController"/>/<see cref="WhiteListsController"/>, <c>TB_RABET</c>
/// carries a real UNIQUE constraint (<c>UK_RABET</c> on
/// <c>(ACCOUNTCODE_ID, RABETTYPE_ID)</c>), mapped centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> to <c>DuplicateKeyException</c> → 409.
///
/// <b>400 also covers FK violations</b>: an <c>AccountCodeId</c>/<c>RabetTypeId</c> that does
/// not reference an existing row violates <c>FK_RABET_ACCOUNTCODE</c>/<c>FK_RABET_TYPE</c> and
/// is mapped centrally to <c>ForeignKeyViolationException</c> → 400 (plain
/// <see cref="ProblemDetails"/>, no <c>errors</c> dictionary — naming the offending field would
/// mean leaking the Oracle constraint name).
/// </summary>
[ApiController]
[Route("api/rabets")]
public sealed class RabetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RabetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new rabet link (<c>TB_RABET</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateRabetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRabetCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateRabetResponse(id));
    }

    /// <summary>
    /// Returns a page of rabet links.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RabetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetRabetsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single rabet link by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RabetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRabetByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing rabet link (<c>TB_RABET</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateRabetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRabetRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRabetCommand(
            id,
            request.RabetTypeId,
            request.AccountCodeId);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateRabetResponse(id));
    }

    /// <summary>
    /// Soft-deletes a rabet link (<c>TB_RABET.ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteRabetCommandHandler"/> XML doc. Returns <b>200</b> with the affected
    /// <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteRabetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteRabetCommand(id), cancellationToken);

        return Ok(new DeleteRabetResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="RabetsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_RABET.ID</c>.</param>
public sealed record CreateRabetResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="RabetsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_RABET.ID</c> that was updated (from the route).</param>
public sealed record UpdateRabetResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="RabetsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_RABET.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteRabetResponse(Guid Id);

/// <summary>
/// Request body for <see cref="RabetsController.Update"/>. Mirrors every field of
/// <see cref="UpdateRabetCommand"/> except <c>Id</c>, which is bound from the route instead.
/// </summary>
public sealed record UpdateRabetRequest(
    Guid? RabetTypeId,
    Guid? AccountCodeId);
