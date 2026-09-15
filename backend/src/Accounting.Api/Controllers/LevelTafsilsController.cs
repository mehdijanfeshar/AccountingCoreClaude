using Accounting.Application.Common;
using Accounting.Application.LevelTafsils.Commands.CreateLevelTafsil;
using Accounting.Application.LevelTafsils.Commands.DeleteLevelTafsil;
using Accounting.Application.LevelTafsils.Commands.UpdateLevelTafsil;
using Accounting.Application.LevelTafsils.Queries;
using Accounting.Application.LevelTafsils.Queries.GetLevelTafsilById;
using Accounting.Application.LevelTafsils.Queries.GetLevelTafsils;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_LEVEL_TAFSIL</c> (Legacy tafsili-level lookup — many other
/// tables, e.g. <c>TB_ACCOUNT_LINK_LEVEL</c>, FK into this table) write and read use cases. Every
/// action does nothing but: build a request → send it through MediatR → map the result to an
/// <see cref="IActionResult"/>. All validation lives in FluentValidation validators (run by
/// <c>ValidationBehavior</c>) and all business rules live in the Application/Domain layers —
/// never here.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update/Delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="AccountCodesController"/>.
///
/// <b>409 Conflict is deliberately NOT declared on <see cref="Create"/>/<see cref="Update"/></b>
/// — <c>TB_LEVEL_TAFSIL</c> carries NO UNIQUE constraint at all, so declaring 409 would be
/// speculation about a constraint that does not exist (mirroring the recorded phase-10 decision
/// applied to <c>VoucherDetailsController</c> and the <see cref="WhiteListsController"/>/
/// <see cref="PreDescribsController"/>/<see cref="BillLogsController"/> precedent).
///
/// <b>400 does NOT cover FK violations either</b> — <c>TB_LEVEL_TAFSIL</c> has no FK of its own
/// (it is a lookup table other tables FK <em>into</em>, not the other way around), so 400 here is
/// FluentValidation failures only.
///
/// ⚠️ <b>Deleting a level does NOT check for dependent rows</b> in tables that FK into this one
/// (e.g. <c>TB_ACCOUNT_LINK_LEVEL</c>) — see <see cref="DeleteLevelTafsilCommand"/> XML doc. This
/// mirrors the recorded open risk on <c>DeleteAccountCodeCommand</c> in CLAUDE.md and is an
/// unmade business decision, not an oversight.
/// </summary>
[ApiController]
[Route("api/level-tafsils")]
public sealed class LevelTafsilsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LevelTafsilsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new tafsili-level lookup entry (<c>TB_LEVEL_TAFSIL</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateLevelTafsilResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLevelTafsilCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateLevelTafsilResponse(id));
    }

    /// <summary>
    /// Returns a page of tafsili-level lookup entries.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LevelTafsilDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetLevelTafsilsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single tafsili-level lookup entry by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LevelTafsilDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetLevelTafsilByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing tafsili-level lookup entry (<c>TB_LEVEL_TAFSIL</c> row).
    /// Exposed as <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate.
    /// <c>Id</c> is taken from the route, never the body. Returns <b>200</b> with the affected
    /// <c>Id</c> in the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateLevelTafsilResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateLevelTafsilRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLevelTafsilCommand(
            id,
            request.LevelCode,
            request.LevelName);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateLevelTafsilResponse(id));
    }

    /// <summary>
    /// Soft-deletes a tafsili-level lookup entry (<c>TB_LEVEL_TAFSIL.ISDELETED = true</c>).
    /// Exposed as <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteLevelTafsilCommandHandler"/> XML doc. Returns <b>200</b> with the
    /// affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteLevelTafsilResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteLevelTafsilCommand(id), cancellationToken);

        return Ok(new DeleteLevelTafsilResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="LevelTafsilsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_LEVEL_TAFSIL.ID</c>.</param>
public sealed record CreateLevelTafsilResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="LevelTafsilsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_LEVEL_TAFSIL.ID</c> that was updated (from the route).</param>
public sealed record UpdateLevelTafsilResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="LevelTafsilsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_LEVEL_TAFSIL.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteLevelTafsilResponse(Guid Id);

/// <summary>
/// Request body for <see cref="LevelTafsilsController.Update"/>. Mirrors every field of
/// <see cref="UpdateLevelTafsilCommand"/> except <c>Id</c>, which is bound from the route
/// instead.
/// </summary>
public sealed record UpdateLevelTafsilRequest(
    string LevelCode,
    string LevelName);
