using Accounting.Application.Accounts.Commands.CreateAccountCode;
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAccountCodeByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }
}

/// <summary>
/// Response body for a successful <see cref="AccountCodesController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_ACCOUNTCODE.ID</c>.</param>
public sealed record CreateAccountCodeResponse(Guid Id);
