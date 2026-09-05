using Accounting.Application.CheckBooks.Commands.CreateCheckBook;
using Accounting.Application.CheckBooks.Commands.DeleteCheckBook;
using Accounting.Application.CheckBooks.Commands.UpdateCheckBook;
using Accounting.Application.CheckBooks.Queries;
using Accounting.Application.CheckBooks.Queries.GetCheckBookById;
using Accounting.Application.CheckBooks.Queries.GetCheckBooks;
using Accounting.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_CHECKBOOK</c> (Legacy bank checkbook master) write and read
/// use cases. Every action does nothing but: build a request → send it through MediatR → map the
/// result to an <see cref="IActionResult"/>. All validation lives in FluentValidation validators
/// (run by <c>ValidationBehavior</c>) and all business rules live in the Application/Domain
/// layers — never here.
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
/// <c>TB_CHECKBOOK</c> carries a real UNIQUE constraint (<c>UK_CHECKBOOK</c> on
/// <c>ACCOUNT_ID, FROMCHECKNUMBER, TOCHECKNUMBER, VAHEDCODE</c>), mapped centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> to <c>DuplicateKeyException</c> → 409.
///
/// <b>400 also covers two mapped FK violations</b>: an <c>AccountId</c>/<c>CheckTypeId</c> that
/// does not reference an existing row violates <c>FK_CHECKBOOK_ACCOUNT</c>/<c>FK_CHECKTYPE</c>
/// and is mapped centrally to <c>ForeignKeyViolationException</c> → 400 (plain
/// <see cref="ProblemDetails"/>, no <c>errors</c> dictionary — naming the offending field would
/// mean leaking the Oracle constraint name).
///
/// ⚠️ <c>CheckBookType</c> is <c>NUMBER(1)</c> typed as <see cref="bool"/>? — an unverified
/// sibling of the confirmed <c>TB_TAFSILI.ISACTIVE</c> enum bug (CLAUDE.md phase 12); see
/// <see cref="CreateCheckBookCommand"/> XML doc. ⚠️ The permanently-embedded child <c>TB_CHECK</c>
/// table is untouched by this controller entirely — soft-deleting a checkbook here does NOT
/// cascade to it, leaving its cheque rows active (a known gap, not fixed here).
/// </summary>
[ApiController]
[Route("api/check-books")]
public sealed class CheckBooksController : ControllerBase
{
    private readonly IMediator _mediator;

    public CheckBooksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new checkbook (<c>TB_CHECKBOOK</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateCheckBookResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCheckBookCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateCheckBookResponse(id));
    }

    /// <summary>
    /// Returns a page of checkbooks.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CheckBookDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCheckBooksQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single checkbook by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CheckBookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCheckBookByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing checkbook (<c>TB_CHECKBOOK</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateCheckBookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateCheckBookRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCheckBookCommand(
            id,
            request.AccountId,
            request.CheckBookTitle,
            request.CheckBookDate,
            request.FromCheckNumber,
            request.ToCheckNumber,
            request.CheckTypeId,
            request.VahedCode,
            request.CheckBookType,
            request.Serial);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateCheckBookResponse(id));
    }

    /// <summary>
    /// Soft-deletes a checkbook (<c>TB_CHECKBOOK.ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteCheckBookCommandHandler"/> XML doc. Returns <b>200</b> with the
    /// affected <c>Id</c> in the body (not 204). Does NOT cascade to <c>TB_CHECK</c> — see the
    /// class-level ⚠️ note above.
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteCheckBookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteCheckBookCommand(id), cancellationToken);

        return Ok(new DeleteCheckBookResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="CheckBooksController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_CHECKBOOK.ID</c>.</param>
public sealed record CreateCheckBookResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="CheckBooksController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_CHECKBOOK.ID</c> that was updated (from the route).</param>
public sealed record UpdateCheckBookResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="CheckBooksController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_CHECKBOOK.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteCheckBookResponse(Guid Id);

/// <summary>
/// Request body for <see cref="CheckBooksController.Update"/>. Mirrors every field of
/// <see cref="UpdateCheckBookCommand"/> except <c>Id</c>, which is bound from the route instead.
/// </summary>
public sealed record UpdateCheckBookRequest(
    Guid AccountId,
    string? CheckBookTitle,
    string CheckBookDate,
    string FromCheckNumber,
    string ToCheckNumber,
    Guid? CheckTypeId,
    string VahedCode,
    bool? CheckBookType,
    string? Serial);
