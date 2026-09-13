using Accounting.Application.Common;
using Accounting.Application.Tafsilis.Commands.CreateTafsili;
using Accounting.Application.Tafsilis.Commands.DeleteTafsili;
using Accounting.Application.Tafsilis.Commands.UpdateTafsili;
using Accounting.Application.Tafsilis.Queries;
using Accounting.Application.Tafsilis.Queries.GetTafsiliById;
using Accounting.Application.Tafsilis.Queries.GetTafsilis;
using Accounting.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_TAFSILI</c> (Legacy detail/subsidiary-ledger account master)
/// write and read use cases. Every action does nothing but: build a request → send it through
/// MediatR → map the result to an <see cref="IActionResult"/>. All validation lives in
/// FluentValidation validators (run by <c>ValidationBehavior</c>) and all business rules live in
/// the Application/Domain layers — never here.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update/Delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="TafsilGroupsController"/>.
///
/// <b>409 Conflict IS declared on <see cref="Create"/>/<see cref="Update"/></b> — <c>TB_TAFSILI</c>
/// carries a real UNIQUE constraint (<c>UK_TASILI</c> on <c>TAFSILI_CODE</c> alone, NOT composite
/// with <c>ISDELETED</c> — see <see cref="CreateTafsiliCommand"/> XML doc), mapped centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> to <c>DuplicateKeyException</c> → 409.
///
/// <b>400 does NOT cover any FK violation</b> — the one real FK on this table
/// (<c>FK_VAHEDCODE</c> to <c>TB_VAHED_INFO</c>) is on the server-assigned <c>VahedCode</c>, never
/// client input, so it can never be violated by a caller.
///
/// <b><see cref="Create"/>/<see cref="Update"/>/<see cref="GetList"/> also declare <c>403
/// Forbidden</c></b> — <c>CreateTafsiliCommand</c>/<c>GetTafsilisQuery</c> implement
/// <c>IVahedScopedCommand</c>/<c>IVahedScopedQuery</c> (mirroring <c>Expenses</c>/<c>BankAccounts</c>),
/// so <c>VahedScopeBehavior</c> throws <c>MissingVahedScopeException</c> → 403 when the caller has
/// no usable unit-scope claim. <c>Update</c> also declares 403 for the same reason even though it
/// carries no <c>VahedCode</c> field itself — it is grouped with Create for consistency of the
/// documented contract; <see cref="GetById"/>/<see cref="Delete"/> do not opt in and never return
/// 403.
///
/// <b>گروه‌تفصیلی linking is embedded in Create/Update, not a separate endpoint</b> — see
/// <see cref="CreateTafsiliCommand"/>/<see cref="UpdateTafsiliCommand"/> XML docs.
/// </summary>
[ApiController]
[Route("api/tafsilis")]
public sealed class TafsilisController : ControllerBase
{
    private readonly IMediator _mediator;

    public TafsilisController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new تفصیلی account (<c>TB_TAFSILI</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateTafsiliResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTafsiliCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateTafsiliResponse(id));
    }

    /// <summary>
    /// Returns a page of تفصیلی accounts belonging to the caller's own organizational unit.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TafsiliDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTafsilisQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single تفصیلی account by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TafsiliDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTafsiliByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing تفصیلی account (<c>TB_TAFSILI</c> row) and reconciles its
    /// گروه‌تفصیلی link set. Exposed as <c>POST {id}/update</c>, not <c>PUT</c> — by explicit
    /// project-owner mandate. <c>Id</c> is taken from the route, never the body. Returns
    /// <b>200</b> with the affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateTafsiliResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTafsiliRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTafsiliCommand(
            id,
            request.TafsiliCode,
            request.TafsiliName,
            request.TafsilDesc,
            request.IsActive,
            request.PersonType,
            request.Owner,
            request.VahedType,
            request.TafsilGroupIds,
            request.TafsilGroupLinkVahedType);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateTafsiliResponse(id));
    }

    /// <summary>
    /// Soft-deletes a تفصیلی account (<c>TB_TAFSILI.ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate. Idempotent:
    /// a row that is already soft-deleted still returns 200. Returns <b>200</b> with the affected
    /// <c>Id</c> in the body (not 204). Does NOT cascade to <c>TB_TAFSIL_LINK_TAFSILGROUP</c> —
    /// see <see cref="DeleteTafsiliCommand"/> XML doc.
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteTafsiliResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTafsiliCommand(id), cancellationToken);

        return Ok(new DeleteTafsiliResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="TafsilisController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_TAFSILI.ID</c>.</param>
public sealed record CreateTafsiliResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="TafsilisController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_TAFSILI.ID</c> that was updated (from the route).</param>
public sealed record UpdateTafsiliResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="TafsilisController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_TAFSILI.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteTafsiliResponse(Guid Id);

/// <summary>
/// Request body for <see cref="TafsilisController.Update"/>. Mirrors every field of
/// <see cref="UpdateTafsiliCommand"/> except <c>Id</c>, which is bound from the route instead.
/// </summary>
public sealed record UpdateTafsiliRequest(
    string TafsiliCode,
    string TafsiliName,
    string? TafsilDesc,
    bool? IsActive,
    bool? PersonType,
    bool? Owner,
    bool? VahedType,
    IReadOnlyList<Guid> TafsilGroupIds,
    VahedCategory? TafsilGroupLinkVahedType = null);
