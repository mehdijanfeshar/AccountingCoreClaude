using Accounting.Application.Common;
using Accounting.Application.PreDescribs.Commands.CreatePreDescrib;
using Accounting.Application.PreDescribs.Commands.UpdatePreDescrib;
using Accounting.Application.PreDescribs.Queries;
using Accounting.Application.PreDescribs.Queries.GetPreDescribById;
using Accounting.Application.PreDescribs.Queries.GetPreDescribs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_PREDESCRIB</c> (Legacy pre-description template, mapped to
/// Oracle table <c>TB_PREDESCRIBS</c>) write and read use cases. Every action does nothing but:
/// build a request → send it through MediatR → map the result to an <see cref="IActionResult"/>.
/// All validation lives in FluentValidation validators (run by <c>ValidationBehavior</c>) and
/// all business rules live in the Application/Domain layers — never here.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update is exposed as <c>POST {id}/update</c>, mirroring
/// <see cref="AccountCodesController"/>.
///
/// <b>There is no Delete action at all, and this is deliberate, not an unfinished feature.</b>
/// <c>TB_PREDESCRIB</c> has no <c>ISDELETED</c> column — locked in by reflection in
/// <c>Accounting.Application.Tests.PreDescribs.PreDescribSchemaAssumptionsTests</c> — and this
/// project never issues physical deletes (see CLAUDE.md). Adding a delete path here would
/// require either a physical <c>DELETE</c> statement (breaking Legacy referential integrity via
/// <c>FK_ACCOUNT</c> and diverging from how the original Legacy application behaves) or an
/// Oracle schema change (adding an <c>ISDELETED</c> column) — and this project never alters the
/// Oracle schema. Neither option is safe, so no delete path exists.
///
/// <b>409 Conflict is deliberately NOT declared on <see cref="Create"/>/<see cref="Update"/></b>
/// — <c>TB_PREDESCRIB</c> carries no UNIQUE constraint at all, so declaring 409 would be
/// speculation about a constraint that does not exist (mirroring the recorded phase-10
/// decision applied to <c>VoucherDetailsController</c>).
///
/// <b><see cref="Create"/>/<see cref="Update"/>/<see cref="GetList"/> also declare <c>403
/// Forbidden</c></b> — <c>CreatePreDescribCommand</c>/<c>UpdatePreDescribCommand</c>/
/// <c>GetPreDescribsQuery</c> all implement <c>IVahedScopedCommand</c>/<c>IVahedScopedQuery</c>,
/// so <c>VahedScopeBehavior</c> throws <c>MissingVahedScopeException</c> → 403 (via
/// <c>GlobalExceptionHandler</c>) when the authenticated caller has no usable unit-scope claim.
/// <see cref="GetById"/> does not opt in and never returns 403 for this reason — see
/// <see cref="UpdatePreDescribCommand"/> XML doc for the explicit scope note on what closing this
/// still leaves open.
/// </summary>
[ApiController]
[Route("api/pre-describs")]
public sealed class PreDescribsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PreDescribsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new pre-description (<c>TB_PREDESCRIB</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePreDescribResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePreDescribCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreatePreDescribResponse(id));
    }

    /// <summary>
    /// Returns a page of pre-descriptions.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PreDescribDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPreDescribsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single pre-description by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PreDescribDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPreDescribByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing pre-description (<c>TB_PREDESCRIB</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdatePreDescribResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePreDescribRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePreDescribCommand(
            id,
            request.AccountId,
            request.Descrip,
            request.FlagVoucher);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdatePreDescribResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="PreDescribsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_PREDESCRIB.ID</c>.</param>
public sealed record CreatePreDescribResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="PreDescribsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_PREDESCRIB.ID</c> that was updated (from the route).</param>
public sealed record UpdatePreDescribResponse(Guid Id);

/// <summary>
/// Request body for <see cref="PreDescribsController.Update"/>. Mirrors every field of
/// <see cref="UpdatePreDescribCommand"/> except <c>Id</c> (bound from the route instead) and
/// <c>VahedCode</c> (server-assigned by <c>VahedScopeBehavior</c> — see
/// <see cref="UpdatePreDescribCommand.VahedCode"/> XML doc — so it is not part of this request
/// body at all, not even as an ignored field).
/// </summary>
public sealed record UpdatePreDescribRequest(
    Guid? AccountId,
    string? Descrip,
    bool? FlagVoucher);
