using Accounting.Application.AccountCodeInterfaces.Commands.CreateAccountCodeInterface;
using Accounting.Application.AccountCodeInterfaces.Commands.DeleteAccountCodeInterface;
using Accounting.Application.AccountCodeInterfaces.Commands.UpdateAccountCodeInterface;
using Accounting.Application.AccountCodeInterfaces.Queries;
using Accounting.Application.AccountCodeInterfaces.Queries.GetAccountCodeInterfaceById;
using Accounting.Application.AccountCodeInterfaces.Queries.GetAccountCodeInterfaces;
using Accounting.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_ACCOUNTCODE_INTERFACE</c> (Legacy interface-flag link on an
/// account code) write and read use cases. Every action does nothing but: build a request →
/// send it through MediatR → map the result to an <see cref="IActionResult"/>. All validation
/// lives in FluentValidation validators and all business rules live in the
/// Application/Domain layers — never here.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b> via the API-wide fallback
/// policy (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate</b>, exactly
/// as in <see cref="AccountCodesController"/>: update/delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>.
///
/// <b>409 Conflict is deliberately NOT declared on <see cref="Create"/>/<see cref="Update"/></b>
/// — <c>TB_ACCOUNTCODE_INTERFACE</c> has no UNIQUE constraint, only the FK
/// <c>FK_INTRFACE_ACCCOUNTCODE</c>. An invalid <c>AccountCodeId</c> surfaces as a 400 via the
/// central ORA-02291 → <c>ForeignKeyViolationException</c> mapping in <c>UnitOfWork</c> — see
/// <see cref="CreateAccountCodeInterfaceCommand"/> XML doc. No pre-check for FK existence is
/// performed, per the recorded decision not to invent a business rule around it.
/// </summary>
[ApiController]
[Route("api/account-code-interfaces")]
public sealed class AccountCodeInterfacesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountCodeInterfacesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new account code interface link (<c>TB_ACCOUNTCODE_INTERFACE</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateAccountCodeInterfaceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAccountCodeInterfaceCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateAccountCodeInterfaceResponse(id));
    }

    /// <summary>
    /// Returns a page of account code interface links.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AccountCodeInterfaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAccountCodeInterfacesQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single account code interface link by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountCodeInterfaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAccountCodeInterfaceByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing account code interface link. Exposed as <c>POST {id}/update</c>,
    /// not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is taken from the route,
    /// never the body. Returns <b>200</b> with the affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateAccountCodeInterfaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAccountCodeInterfaceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAccountCodeInterfaceCommand(
            id,
            request.Type,
            request.AccountCodeId);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateAccountCodeInterfaceResponse(id));
    }

    /// <summary>
    /// Soft-deletes an account code interface link (<c>ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200. Returns <b>200</b>
    /// with the affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteAccountCodeInterfaceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteAccountCodeInterfaceCommand(id), cancellationToken);

        return Ok(new DeleteAccountCodeInterfaceResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="AccountCodeInterfacesController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_ACCOUNTCODE_INTERFACE.ID</c>.</param>
public sealed record CreateAccountCodeInterfaceResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="AccountCodeInterfacesController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNTCODE_INTERFACE.ID</c> that was updated (from the route).</param>
public sealed record UpdateAccountCodeInterfaceResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="AccountCodeInterfacesController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNTCODE_INTERFACE.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteAccountCodeInterfaceResponse(Guid Id);

/// <summary>
/// Request body for <see cref="AccountCodeInterfacesController.Update"/>. Mirrors every field of
/// <see cref="UpdateAccountCodeInterfaceCommand"/> except <c>Id</c>, which is bound from the
/// route instead.
/// </summary>
public sealed record UpdateAccountCodeInterfaceRequest(
    bool Type,
    Guid AccountCodeId);
