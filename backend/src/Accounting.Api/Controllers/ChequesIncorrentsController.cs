using Accounting.Application.ChequesIncorrents.Commands.CreateChequesIncorrent;
using Accounting.Application.ChequesIncorrents.Commands.DeleteChequesIncorrent;
using Accounting.Application.ChequesIncorrents.Commands.UpdateChequesIncorrent;
using Accounting.Application.ChequesIncorrents.Queries;
using Accounting.Application.ChequesIncorrents.Queries.GetChequesIncorrentById;
using Accounting.Application.ChequesIncorrents.Queries.GetChequesIncorrents;
using Accounting.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_CHEQUES_INCORRENT</c> (Legacy "dishonored/incorrect
/// cheques" register) write and read use cases. Every action does nothing but: build a request
/// → send it through MediatR → map the result to an <see cref="IActionResult"/>. All validation
/// lives in FluentValidation validators (run by <c>ValidationBehavior</c>) and all business
/// rules live in the Application/Domain layers — never here.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update/Delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="WorkShopsController"/>.
///
/// <b><see cref="Create"/>/<see cref="Update"/>/<see cref="GetList"/> also declare <c>403
/// Forbidden</c></b> — <c>CreateChequesIncorrentCommand</c>/<c>UpdateChequesIncorrentCommand</c>/
/// <c>GetChequesIncorrentsQuery</c> all implement <c>IVahedScopedCommand</c>/
/// <c>IVahedScopedQuery</c>, so <c>VahedScopeBehavior</c> throws <c>MissingVahedScopeException</c>
/// → 403 (via <c>GlobalExceptionHandler</c>) when the authenticated caller has no usable
/// unit-scope claim. <see cref="GetById"/>/<see cref="Delete"/> do not opt in and never return
/// 403 for this reason.
///
/// <b>409 Conflict is deliberately NOT declared anywhere on this controller</b> — unlike
/// <see cref="BankAccountsController"/> and <see cref="CheckBooksController"/>,
/// <c>TB_CHEQUES_INCORRENT</c> carries NO UNIQUE constraint at all, so declaring 409 here would
/// be speculation about a constraint that does not exist — the same judgement already applied
/// to <c>TB_VOUCHERSDETAIL</c> in phase 10 (see <see cref="VoucherDetailsController"/>).
///
/// ⚠️ <c>CheckId</c> has NO FK at all in the Legacy schema — an invalid value is written
/// silently and the central FK→400 mapping used elsewhere in this project does not apply here;
/// see <see cref="CreateChequesIncorrentCommand"/> XML doc. ⚠️ <c>Creditor</c> is an amount
/// column whose data type (<see cref="decimal"/> vs <c>long</c>) is an open project question —
/// see the same XML doc.
/// </summary>
[ApiController]
[Route("api/cheques-incorrents")]
public sealed class ChequesIncorrentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChequesIncorrentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new dishonored/incorrect cheque record (<c>TB_CHEQUES_INCORRENT</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateChequesIncorrentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateChequesIncorrentCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateChequesIncorrentResponse(id));
    }

    /// <summary>
    /// Returns a page of dishonored/incorrect cheque records.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ChequesIncorrentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetChequesIncorrentsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single dishonored/incorrect cheque record by <c>ID</c>, or 404 when it does not
    /// exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ChequesIncorrentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetChequesIncorrentByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing dishonored/incorrect cheque record
    /// (<c>TB_CHEQUES_INCORRENT</c> row). Exposed as <c>POST {id}/update</c>, not <c>PUT</c> —
    /// by explicit project-owner mandate. <c>Id</c> is taken from the route, never the body.
    /// Returns <b>200</b> with the affected <c>Id</c> in the body (not 204), mirroring every
    /// other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateChequesIncorrentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateChequesIncorrentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateChequesIncorrentCommand(
            id,
            request.CheckId,
            request.DocNum,
            request.DocDate,
            request.CheqNo,
            request.CheqDate,
            request.PaperDesc,
            request.PayTo,
            request.RecivDate,
            request.AccountNumber,
            request.Creditor,
            request.Year);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateChequesIncorrentResponse(id));
    }

    /// <summary>
    /// Soft-deletes a dishonored/incorrect cheque record
    /// (<c>TB_CHEQUES_INCORRENT.ISDELETED = true</c>). Exposed as <c>POST {id}/delete</c>, not
    /// <c>DELETE</c> — by explicit project-owner mandate. Idempotent: a row that is already
    /// soft-deleted still returns 200 — see <see cref="DeleteChequesIncorrentCommandHandler"/>
    /// XML doc. Returns <b>200</b> with the affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteChequesIncorrentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteChequesIncorrentCommand(id), cancellationToken);

        return Ok(new DeleteChequesIncorrentResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="ChequesIncorrentsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_CHEQUES_INCORRENT.ID</c>.</param>
public sealed record CreateChequesIncorrentResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="ChequesIncorrentsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_CHEQUES_INCORRENT.ID</c> that was updated (from the route).</param>
public sealed record UpdateChequesIncorrentResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="ChequesIncorrentsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_CHEQUES_INCORRENT.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteChequesIncorrentResponse(Guid Id);

/// <summary>
/// Request body for <see cref="ChequesIncorrentsController.Update"/>. Mirrors every field of
/// <see cref="UpdateChequesIncorrentCommand"/> except <c>Id</c> (bound from the route instead)
/// and <c>VahedCode</c> (server-assigned by <c>VahedScopeBehavior</c> — see
/// <see cref="UpdateChequesIncorrentCommand.VahedCode"/> XML doc — so it is not part of this
/// request body at all, not even as an ignored field).
/// </summary>
public sealed record UpdateChequesIncorrentRequest(
    Guid? CheckId,
    string DocNum,
    string DocDate,
    string CheqNo,
    string CheqDate,
    string? PaperDesc,
    string? PayTo,
    string? RecivDate,
    string AccountNumber,
    decimal Creditor,
    string Year);
