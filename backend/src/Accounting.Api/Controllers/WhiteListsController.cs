using Accounting.Application.Common;
using Accounting.Application.WhiteLists.Commands.CreateWhiteList;
using Accounting.Application.WhiteLists.Commands.DeleteWhiteList;
using Accounting.Application.WhiteLists.Commands.UpdateWhiteList;
using Accounting.Application.WhiteLists.Queries;
using Accounting.Application.WhiteLists.Queries.GetWhiteListById;
using Accounting.Application.WhiteLists.Queries.GetWhiteLists;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_WHITELIST</c> (Legacy account-code allow-list entry) write
/// and read use cases. Every action does nothing but: build a request → send it through
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
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="AccountCodesController"/>.
///
/// <b>409 Conflict is deliberately NOT declared on <see cref="Create"/>/<see cref="Update"/></b>
/// — unlike <see cref="WhiteAndBlackListsController"/>/<see cref="RabetsController"/>,
/// <c>TB_WHITELIST</c> carries NO UNIQUE constraint at all, so declaring 409 would be
/// speculation about a constraint that does not exist (mirroring the recorded phase-10
/// decision applied to <c>VoucherDetailsController</c>).
///
/// <b>400 also covers FK violations</b>: an <c>AccountCodeId</c>/<c>VahedTypeId</c>/
/// <c>VahedInfoId</c> that does not reference an existing row violates
/// <c>FK_ACCOUNTCODE_LINK_WHITELIST</c>/<c>FK_VAHEDTYPE_LINK_WHITELIST</c>/
/// <c>FK_VAHEDINFO_LINK_WHITELIST</c> and is mapped centrally to
/// <c>ForeignKeyViolationException</c> → 400 (plain <see cref="ProblemDetails"/>, no
/// <c>errors</c> dictionary — naming the offending field would mean leaking the Oracle
/// constraint name).
/// </summary>
[ApiController]
[Route("api/white-lists")]
public sealed class WhiteListsController : ControllerBase
{
    private readonly IMediator _mediator;

    public WhiteListsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new allow-list entry (<c>TB_WHITELIST</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateWhiteListResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateWhiteListCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateWhiteListResponse(id));
    }

    /// <summary>
    /// Returns a page of allow-list entries.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WhiteListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetWhiteListsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single allow-list entry by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WhiteListDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWhiteListByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing allow-list entry (<c>TB_WHITELIST</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateWhiteListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateWhiteListRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateWhiteListCommand(
            id,
            request.AccountCodeId,
            request.VahedTypeId,
            request.VahedInfoId,
            request.FromAuthorizedDate,
            request.ToAuthorizedDate,
            request.FromLimitationDate,
            request.ToLimitationDate);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateWhiteListResponse(id));
    }

    /// <summary>
    /// Soft-deletes an allow-list entry (<c>TB_WHITELIST.ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteWhiteListCommandHandler"/> XML doc. Returns <b>200</b> with the affected
    /// <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteWhiteListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteWhiteListCommand(id), cancellationToken);

        return Ok(new DeleteWhiteListResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="WhiteListsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_WHITELIST.ID</c>.</param>
public sealed record CreateWhiteListResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="WhiteListsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_WHITELIST.ID</c> that was updated (from the route).</param>
public sealed record UpdateWhiteListResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="WhiteListsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_WHITELIST.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteWhiteListResponse(Guid Id);

/// <summary>
/// Request body for <see cref="WhiteListsController.Update"/>. Mirrors every field of
/// <see cref="UpdateWhiteListCommand"/> except <c>Id</c>, which is bound from the route instead.
/// </summary>
public sealed record UpdateWhiteListRequest(
    Guid AccountCodeId,
    Guid? VahedTypeId,
    Guid? VahedInfoId,
    string? FromAuthorizedDate,
    string? ToAuthorizedDate,
    string? FromLimitationDate,
    string? ToLimitationDate);
