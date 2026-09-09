using Accounting.Application.Common;
using Accounting.Application.RevolvingFunds.Commands.CreateRevolvingFund;
using Accounting.Application.RevolvingFunds.Commands.DeleteRevolvingFund;
using Accounting.Application.RevolvingFunds.Commands.UpdateRevolvingFund;
using Accounting.Application.RevolvingFunds.Queries;
using Accounting.Application.RevolvingFunds.Queries.GetRevolvingFundById;
using Accounting.Application.RevolvingFunds.Queries.GetRevolvingFunds;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_REVOLVING_FUND</c> (Legacy revolving fund / تنخواه master)
/// write and read use cases. Every action does nothing but: build a request → send it through
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
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="WorkShopsController"/>.
///
/// <b>409 Conflict IS declared on <see cref="Create"/>/<see cref="Update"/></b> —
/// <c>TB_REVOLVING_FUND</c> carries a real UNIQUE constraint (<c>UK_REVOLVING_CODE</c> on
/// <c>CODE, VAHEDCODE, YEAR</c>), mapped centrally by <c>UnitOfWork.SaveChangesAsync</c> to
/// <c>DuplicateKeyException</c> → 409.
///
/// <b>400 also covers a mapped FK violation</b>: an <c>AccountCodeId</c> that does not reference
/// an existing row violates <c>FK_ACCOUNTCODE_REVOLVING</c> and is mapped centrally to
/// <c>ForeignKeyViolationException</c> → 400 (plain <see cref="ProblemDetails"/>, no
/// <c>errors</c> dictionary — naming the offending field would mean leaking the Oracle
/// constraint name).
///
/// ⚠️ This controller's child table <c>TB_REVOLVINGFUND_LINK_TAFSILI</c> is permanently
/// embedded per the team rule that every <c>*_LINK_TAFSIL*</c> table never gets an independent
/// write path — no route, repository, or cascade exists here for it, by design.
///
/// <b><see cref="Create"/>/<see cref="Update"/>/<see cref="GetList"/> also declare <c>403
/// Forbidden</c></b> — <c>CreateRevolvingFundCommand</c>/<c>UpdateRevolvingFundCommand</c>/
/// <c>GetRevolvingFundsQuery</c> all implement <c>IVahedScopedCommand</c>/<c>IVahedScopedQuery</c>,
/// so <c>VahedScopeBehavior</c> throws <c>MissingVahedScopeException</c> → 403 (via
/// <c>GlobalExceptionHandler</c>) when the authenticated caller has no usable unit-scope claim.
/// <see cref="GetById"/>/<see cref="Delete"/> do not opt in and never return 403 for this reason —
/// see <see cref="UpdateRevolvingFundCommand"/> XML doc for the explicit scope note on what
/// closing this still leaves open.
/// </summary>
[ApiController]
[Route("api/revolving-funds")]
public sealed class RevolvingFundsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RevolvingFundsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new revolving fund (<c>TB_REVOLVING_FUND</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateRevolvingFundResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRevolvingFundCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateRevolvingFundResponse(id));
    }

    /// <summary>
    /// Returns a page of revolving funds.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RevolvingFundDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetRevolvingFundsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single revolving fund by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RevolvingFundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetRevolvingFundByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing revolving fund (<c>TB_REVOLVING_FUND</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateRevolvingFundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRevolvingFundRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRevolvingFundCommand(
            id,
            request.Code,
            request.Name,
            request.Description,
            request.DefaultAmount,
            request.AccountCodeId,
            request.Year);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateRevolvingFundResponse(id));
    }

    /// <summary>
    /// Soft-deletes a revolving fund (<c>TB_REVOLVING_FUND.ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteRevolvingFundCommandHandler"/> XML doc. Returns <b>200</b> with the
    /// affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteRevolvingFundResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteRevolvingFundCommand(id), cancellationToken);

        return Ok(new DeleteRevolvingFundResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="RevolvingFundsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_REVOLVING_FUND.ID</c>.</param>
public sealed record CreateRevolvingFundResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="RevolvingFundsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_REVOLVING_FUND.ID</c> that was updated (from the route).</param>
public sealed record UpdateRevolvingFundResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="RevolvingFundsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_REVOLVING_FUND.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteRevolvingFundResponse(Guid Id);

/// <summary>
/// Request body for <see cref="RevolvingFundsController.Update"/>. Mirrors every field of
/// <see cref="UpdateRevolvingFundCommand"/> except <c>Id</c> (bound from the route instead) and
/// <c>VahedCode</c> (server-assigned by <c>VahedScopeBehavior</c> — see
/// <see cref="UpdateRevolvingFundCommand.VahedCode"/> XML doc — so it is not part of this request
/// body at all, not even as an ignored field).
/// </summary>
public sealed record UpdateRevolvingFundRequest(
    string Code,
    string Name,
    string? Description,
    decimal? DefaultAmount,
    Guid? AccountCodeId,
    string? Year);
