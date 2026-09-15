using Accounting.Application.AccountExceptions.Commands.CreateAccountException;
using Accounting.Application.AccountExceptions.Commands.DeleteAccountException;
using Accounting.Application.AccountExceptions.Commands.UpdateAccountException;
using Accounting.Application.AccountExceptions.Queries;
using Accounting.Application.AccountExceptions.Queries.GetAccountExceptionById;
using Accounting.Application.AccountExceptions.Queries.GetAccountExceptions;
using Accounting.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_ACCOUNTEXCEPTION</c> (Legacy per-unit-type exception rule
/// on an account code) write and read use cases. Every action does nothing but: build a
/// request → send it through MediatR → map the result to an <see cref="IActionResult"/>.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b> via the API-wide fallback
/// policy (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate</b>, exactly
/// as in <see cref="AccountCodesController"/>: update/delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>.
///
/// <b>409 Conflict is deliberately NOT declared on <see cref="Create"/>/<see cref="Update"/></b>
/// — <c>TB_ACCOUNTEXCEPTION</c> has no UNIQUE constraint, only the FKs
/// <c>FK_EXCEPTION_ACCOUNTCODE</c> and <c>FK_ACCOUNTEXCEPTION_VAHEDTYPE</c>. An invalid
/// <c>AccountCoeId</c>/<c>VahedTypeId</c> surfaces as a 400 via the central ORA-02291 →
/// <c>ForeignKeyViolationException</c> mapping in <c>UnitOfWork</c>. No pre-check for FK
/// existence is performed.
/// </summary>
[ApiController]
[Route("api/account-exceptions")]
public sealed class AccountExceptionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountExceptionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new account exception rule (<c>TB_ACCOUNTEXCEPTION</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateAccountExceptionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAccountExceptionCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateAccountExceptionResponse(id));
    }

    /// <summary>
    /// Returns a page of account exception rules.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AccountExceptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAccountExceptionsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single account exception rule by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountExceptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAccountExceptionByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing account exception rule. Exposed as <c>POST {id}/update</c>,
    /// not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is taken from the route,
    /// never the body. Returns <b>200</b> with the affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateAccountExceptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAccountExceptionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAccountExceptionCommand(
            id,
            request.AccountCoeId,
            request.VahedTypeId);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateAccountExceptionResponse(id));
    }

    /// <summary>
    /// Soft-deletes an account exception rule (<c>ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200. Returns <b>200</b>
    /// with the affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteAccountExceptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteAccountExceptionCommand(id), cancellationToken);

        return Ok(new DeleteAccountExceptionResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="AccountExceptionsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_ACCOUNTEXCEPTION.ID</c>.</param>
public sealed record CreateAccountExceptionResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="AccountExceptionsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNTEXCEPTION.ID</c> that was updated (from the route).</param>
public sealed record UpdateAccountExceptionResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="AccountExceptionsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNTEXCEPTION.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteAccountExceptionResponse(Guid Id);

/// <summary>
/// Request body for <see cref="AccountExceptionsController.Update"/>. Mirrors every field of
/// <see cref="UpdateAccountExceptionCommand"/> except <c>Id</c>, which is bound from the route
/// instead.
/// </summary>
public sealed record UpdateAccountExceptionRequest(
    Guid AccountCoeId,
    Guid VahedTypeId);
