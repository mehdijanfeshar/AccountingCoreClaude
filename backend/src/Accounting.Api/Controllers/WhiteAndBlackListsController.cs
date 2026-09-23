using Accounting.Application.Common;
using Accounting.Application.WhiteAndBlackLists.Commands.BlacklistWhiteAndBlackList;
using Accounting.Application.WhiteAndBlackLists.Commands.CreateWhiteAndBlackList;
using Accounting.Application.WhiteAndBlackLists.Commands.CreateWhiteAndBlackListsBulk;
using Accounting.Application.WhiteAndBlackLists.Commands.ReactivateWhiteAndBlackList;
using Accounting.Application.WhiteAndBlackLists.Commands.DeleteWhiteAndBlackList;
using Accounting.Application.WhiteAndBlackLists.Commands.UpdateWhiteAndBlackList;
using Accounting.Application.WhiteAndBlackLists.Queries;
using Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackListById;
using Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackLists;
using Accounting.Domain.ValueObjects;
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
/// <c>State</c> (the <c>STATE</c> column, <c>NUMBER(1)</c>) is modeled as the nullable
/// <see cref="WhiteBlackListState"/> enum — see <see cref="CreateWhiteAndBlackListCommand.State"/>
/// XML doc for the resolution reference (§24-1, phase 27 batch 3).
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
    /// Grants a set of account codes for a set of unit types in one transaction — the
    /// «افزودن دسترسی جدید» dialog. The full cartesian product is written; combinations that
    /// already exist are skipped rather than failing the request, and the response reports both
    /// counts. See <see cref="CreateWhiteAndBlackListsBulkCommand"/> for why there is one date
    /// pair here rather than four.
    ///
    /// 409 is still declared: the skip pass is a pre-check, not a lock, so a row inserted
    /// concurrently between it and the save surfaces as the central duplicate-key mapping.
    /// </summary>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(CreateWhiteAndBlackListsBulkResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateBulk(
        [FromBody] CreateWhiteAndBlackListsBulkRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateWhiteAndBlackListsBulkCommand(
            request.AccountCodeIds,
            request.VahedTypeIds,
            request.FromDate,
            request.ToDate,
            request.State);

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a page of allow/deny-list entries, optionally narrowed by the filter panel of the
    /// «دسترسی کدینگ حسابداری» screen. Every filter is optional and they combine with AND; the
    /// four date parameters are range bounds rather than equality — see
    /// <see cref="GetWhiteAndBlackListsQuery"/>.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<WhiteAndBlackListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? accountCodeId = null,
        [FromQuery] Guid? vahedTypeId = null,
        [FromQuery] WhiteBlackListState? state = null,
        [FromQuery] string? fromAuthorizedDate = null,
        [FromQuery] string? toAuthorizedDate = null,
        [FromQuery] string? fromLimitationDate = null,
        [FromQuery] string? toLimitationDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetWhiteAndBlackListsQuery(
            pageNumber,
            pageSize,
            accountCodeId,
            vahedTypeId,
            state,
            fromAuthorizedDate,
            toAuthorizedDate,
            fromLimitationDate,
            toLimitationDate);

        var result = await _mediator.Send(query, cancellationToken);

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

    /// <summary>
    /// Moves an entry to «غیرمجاز» and clears all four of its date columns — the «غیرفعال‌سازی»
    /// action on the grid. Takes no body: what happens to the dates is part of the transition,
    /// not a caller choice (see <see cref="BlacklistWhiteAndBlackListCommand"/>).
    ///
    /// Idempotent — blacklisting an already-blacklisted row returns 200.
    /// </summary>
    [HttpPost("{id:guid}/blacklist")]
    [ProducesResponseType(typeof(BlacklistWhiteAndBlackListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Blacklist(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new BlacklistWhiteAndBlackListCommand(id), cancellationToken);

        return Ok(new BlacklistWhiteAndBlackListResponse(id));
    }

    /// <summary>
    /// Brings a blacklisted entry back into service with a new state and date range — the
    /// «فعال سازی مجدد» dialog. <c>State</c> may not be «غیرمجاز»; use
    /// <see cref="Blacklist"/> for that.
    /// </summary>
    [HttpPost("{id:guid}/reactivate")]
    [ProducesResponseType(typeof(ReactivateWhiteAndBlackListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Reactivate(
        Guid id,
        [FromBody] ReactivateWhiteAndBlackListRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ReactivateWhiteAndBlackListCommand(
            id,
            request.State,
            request.FromDate,
            request.ToDate);

        await _mediator.Send(command, cancellationToken);

        return Ok(new ReactivateWhiteAndBlackListResponse(id));
    }
}

/// <summary>
/// Request body for <see cref="WhiteAndBlackListsController.CreateBulk"/>.
/// </summary>
/// <param name="AccountCodeIds">The معین rows to grant.</param>
/// <param name="VahedTypeIds">The unit types to grant them for.</param>
/// <param name="FromDate">Start of the range, zero-padded <c>YYYYMMDD</c> Jalali text.</param>
/// <param name="ToDate">End of the range, zero-padded <c>YYYYMMDD</c> Jalali text.</param>
/// <param name="State">Which date pair the range lands in — «مجاز» or «فقط سیستمی».</param>
public sealed record CreateWhiteAndBlackListsBulkRequest(
    IReadOnlyList<Guid> AccountCodeIds,
    IReadOnlyList<Guid> VahedTypeIds,
    string? FromDate,
    string? ToDate,
    WhiteBlackListState State);

/// <summary>
/// Response body for a successful <see cref="WhiteAndBlackListsController.Blacklist"/> call.
/// </summary>
/// <param name="Id">The <c>TB_WHITEANDBLACKLIST.ID</c> that was blacklisted (from the route).</param>
public sealed record BlacklistWhiteAndBlackListResponse(Guid Id);

/// <summary>
/// Request body for <see cref="WhiteAndBlackListsController.Reactivate"/>. <c>Id</c> is
/// deliberately absent — it comes from the route, never the body.
/// </summary>
/// <param name="State">The state to return to. «غیرمجاز» is rejected with 400.</param>
/// <param name="FromDate">Start of the new range, zero-padded <c>YYYYMMDD</c> Jalali text.</param>
/// <param name="ToDate">End of the new range, zero-padded <c>YYYYMMDD</c> Jalali text.</param>
public sealed record ReactivateWhiteAndBlackListRequest(
    WhiteBlackListState State,
    string? FromDate,
    string? ToDate);

/// <summary>
/// Response body for a successful <see cref="WhiteAndBlackListsController.Reactivate"/> call.
/// </summary>
/// <param name="Id">The <c>TB_WHITEANDBLACKLIST.ID</c> that was reactivated (from the route).</param>
public sealed record ReactivateWhiteAndBlackListResponse(Guid Id);

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
    WhiteBlackListState? State);
