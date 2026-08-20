using Accounting.Application.Accounts.Commands.CreateAccountCode;
using Accounting.Application.Accounts.Commands.DeleteAccountCode;
using Accounting.Application.Accounts.Commands.UpdateAccountCode;
using Accounting.Application.Accounts.Queries;
using Accounting.Application.Accounts.Queries.GetAccountCodeById;
using Accounting.Application.Accounts.Queries.GetAccountCodes;
using Accounting.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_ACCOUNTCODE</c> (Legacy chart-of-accounts coding node)
/// write and read use cases. Every action does nothing but: build a request → send it through
/// MediatR → map the result to an <see cref="IActionResult"/>. All validation lives in
/// FluentValidation validators (run by <c>ValidationBehavior</c>) and all business rules live
/// in the Application/Domain layers — never here.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>). That response is
/// produced by the authentication middleware — before MVC/MediatR ever run — not by
/// <c>GlobalExceptionHandler</c>, but it is still documented via <c>[ProducesResponseType]</c>
/// on every action for an accurate, uniform OpenAPI contract.
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> The network infrastructure this API runs behind does not
/// allow <c>PUT</c>/<c>DELETE</c> verbs, so update/delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c> instead. See the XML doc on <see cref="Update"/>
/// and <see cref="Delete"/> for the exact route shape and response contract.
/// </summary>
[ApiController]
[Route("api/account-codes")]
public sealed class AccountCodesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountCodesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new account code (<c>TB_ACCOUNTCODE</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateAccountCodeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAccountCodeCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateAccountCodeResponse(id));
    }

    /// <summary>
    /// Returns a page of account codes.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AccountCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAccountCodesQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single account code by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountCodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAccountCodeByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing account code (<c>TB_ACCOUNTCODE</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate, <c>PUT</c>
    /// and <c>DELETE</c> are not usable in this environment, so this action deliberately deviates
    /// from REST verb semantics. Not <c>POST /api/account-codes/{id}</c> (without a trailing
    /// segment) either, because that would be ambiguous with "create a sub-resource under this
    /// id" now that <c>POST /api/account-codes</c> (create) already exists. See
    /// <see cref="UpdateAccountCodeCommand"/> XML doc for the field-level replace-vs-patch
    /// rationale. <c>Id</c> is taken from the route, never the body, so there is no possibility
    /// of a route/body id mismatch. Returns <b>200</b> with the affected <c>Id</c> in the body
    /// (not 204) so every write action on this controller has a uniform response shape.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateAccountCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAccountCodeRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateAccountCodeCommand(
            id,
            request.TypeCode,
            request.ParentId,
            request.AccCode,
            request.AccCodeName,
            request.TypeActivity,
            request.SourceAndConsumeId,
            request.IdentyGroupsId,
            request.TypeAccCode,
            request.MoInforClose,
            request.TypeAction);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateAccountCodeResponse(id));
    }

    /// <summary>
    /// Soft-deletes an account code (<c>TB_ACCOUNTCODE.ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate, <c>PUT</c>
    /// and <c>DELETE</c> are not usable in this environment, so this action deliberately deviates
    /// from REST verb semantics. Idempotent: a row that is already soft-deleted still returns
    /// 200, matching HTTP DELETE semantics — see <see cref="DeleteAccountCodeCommandHandler"/>
    /// XML doc. Returns <b>200</b> with the affected <c>Id</c> in the body (not 204) so every
    /// write action on this controller has a uniform response shape.
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteAccountCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteAccountCodeCommand(id), cancellationToken);

        return Ok(new DeleteAccountCodeResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="AccountCodesController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_ACCOUNTCODE.ID</c>.</param>
public sealed record CreateAccountCodeResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="AccountCodesController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNTCODE.ID</c> that was updated (from the route).</param>
public sealed record UpdateAccountCodeResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="AccountCodesController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNTCODE.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteAccountCodeResponse(Guid Id);

/// <summary>
/// Request body for <see cref="AccountCodesController.Update"/>. Mirrors every field of
/// <see cref="UpdateAccountCodeCommand"/> except <c>Id</c>, which is bound from the route
/// instead — this deliberately prevents a route id/body id mismatch from ever reaching the
/// handler.
/// </summary>
public sealed record UpdateAccountCodeRequest(
    bool? TypeCode,
    Guid? ParentId,
    string AccCode,
    string AccCodeName,
    bool? TypeActivity,
    Guid? SourceAndConsumeId,
    Guid? IdentyGroupsId,
    bool? TypeAccCode,
    string? MoInforClose,
    bool? TypeAction);
