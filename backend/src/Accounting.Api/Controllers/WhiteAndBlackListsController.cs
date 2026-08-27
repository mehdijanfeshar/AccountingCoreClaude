using Accounting.Application.Common;
using Accounting.Application.WhiteAndBlackLists.Commands.CreateWhiteAndBlackList;
using Accounting.Application.WhiteAndBlackLists.Commands.DeleteWhiteAndBlackList;
using Accounting.Application.WhiteAndBlackLists.Commands.UpdateWhiteAndBlackList;
using Accounting.Application.WhiteAndBlackLists.Queries;
using Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackListById;
using Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackLists;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_WHITEANDBLACKLIST</c> (Legacy account-code allow/deny-list
/// entry) write and read use cases. Every action does nothing but: build a request → send it
/// through MediatR → map the result to an <see cref="IActionResult"/>. All validation lives in
/// FluentValidation validators (run by <c>ValidationBehavior</c>) and all business rules live in
/// the Application/Domain layers — never here.
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
/// <see cref="WhiteListsController"/>/<see cref="PreDescribsController"/>, <c>TB_WHITEANDBLACKLIST</c>
/// carries a real UNIQUE constraint (<c>UK_WHITEANDBLACKLIST</c> on
/// <c>(ACCOUNTCODE_ID, VAHEDTYPE_ID, FROMAUTHORIZEDDATE, TOAUTHORIZEDDATE)</c>), mapped centrally
/// by <c>UnitOfWork.SaveChangesAsync</c> to <c>DuplicateKeyException</c> → 409.
///
/// <b>400 also covers FK violations</b>: an <c>AccountCodeId</c>/<c>VahedTypeId</c> that does not
/// reference an existing row violates <c>FK_ACCOUNTCODE_LINK_WHITELISTS</c>/
/// <c>FK_VAHEDTYPE_WHITEANDBLACKLIST</c> and is mapped centrally to
/// <c>ForeignKeyViolationException</c> → 400 (plain <see cref="ProblemDetails"/>, no
/// <c>errors</c> dictionary — naming the offending field would mean leaking the Oracle
/// constraint name).
///
/// <b>Known contract caveat on <c>State</c>:</b> the underlying Oracle column
/// (<c>STATE</c>, <c>NUMBER(1)</c>) is documented as a 3-valued enum, not a boolean — see
/// <see cref="CreateWhiteAndBlackListCommand.State"/> XML doc. Fixing this is a separate,
/// explicitly out-of-scope task.
/// </summary>
[ApiController]
[Route("api/white-and-black-lists")]
public sealed class WhiteAndBlackListsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WhiteAndBlackListsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new allow/deny-list entry (<c>TB_WHITEANDBLACKLIST</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateWhiteAndBlackListResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWhiteAndBlackListCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateWhiteAndBlackListResponse(id));
    }

    /// <summary>
    /// Returns a page of allow/deny-list entries.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WhiteAndBlackListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetWhiteAndBlackListsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single allow/deny-list entry by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WhiteAndBlackListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWhiteAndBlackListByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing allow/deny-list entry (<c>TB_WHITEANDBLACKLIST</c> row).
    /// Exposed as <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate.
    /// <c>Id</c> is taken from the route, never the body. Returns <b>200</b> with the affected
    /// <c>Id</c> in the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateWhiteAndBlackListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateWhiteAndBlackListRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWhiteAndBlackListCommand(
            id,
            request.AccountCodeId,
            request.VahedTypeId,
            request.FromAuthorizedDate,
            request.ToAuthorizedDate,
            request.FromLimitationDate,
            request.ToLimitationDate,
            request.State);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateWhiteAndBlackListResponse(id));
    }

    /// <summary>
    /// Soft-deletes an allow/deny-list entry (<c>TB_WHITEANDBLACKLIST.ISDELETED = true</c>).
    /// Exposed as <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteWhiteAndBlackListCommandHandler"/> XML doc. Returns <b>200</b> with the
    /// affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteWhiteAndBlackListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteWhiteAndBlackListCommand(id), cancellationToken);

        return Ok(new DeleteWhiteAndBlackListResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="WhiteAndBlackListsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_WHITEANDBLACKLIST.ID</c>.</param>
public sealed record CreateWhiteAndBlackListResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="WhiteAndBlackListsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_WHITEANDBLACKLIST.ID</c> that was updated (from the route).</param>
public sealed record UpdateWhiteAndBlackListResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="WhiteAndBlackListsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_WHITEANDBLACKLIST.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteWhiteAndBlackListResponse(Guid Id);

/// <summary>
/// Request body for <see cref="WhiteAndBlackListsController.Update"/>. Mirrors every field of
/// <see cref="UpdateWhiteAndBlackListCommand"/> except <c>Id</c>, which is bound from the route
/// instead.
/// </summary>
public sealed record UpdateWhiteAndBlackListRequest(
    Guid AccountCodeId,
    Guid? VahedTypeId,
    string? FromAuthorizedDate,
    string? ToAuthorizedDate,
    string? FromLimitationDate,
    string? ToLimitationDate,
    bool? State);
