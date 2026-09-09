using Accounting.Application.Common;
using Accounting.Application.Expenses.Commands.CreateExpense;
using Accounting.Application.Expenses.Commands.DeleteExpense;
using Accounting.Application.Expenses.Commands.UpdateExpense;
using Accounting.Application.Expenses.Queries;
using Accounting.Application.Expenses.Queries.GetExpenseById;
using Accounting.Application.Expenses.Queries.GetExpenses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_EXPENCE</c> (Legacy expense-type master) write and read use
/// cases. Every action does nothing but: build a request → send it through MediatR → map the
/// result to an <see cref="IActionResult"/>. All validation lives in FluentValidation validators
/// (run by <c>ValidationBehavior</c>) and all business rules live in the Application/Domain
/// layers — never here.
///
/// ⚠️ The Oracle table name is misspelled (<c>TB_EXPENCE</c>). Per project convention (precedent:
/// <c>ChequeType</c> for <c>TB_CHECK_TYPE</c>), every CLR-side identifier here — this controller's
/// name, the route (<c>/api/expenses</c>), and every DTO/Command property — is normalised to the
/// correct spelling <c>Expense</c>. See <see cref="CreateExpenseCommand"/> XML doc.
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
/// <c>TB_EXPENCE</c> carries a real UNIQUE constraint (<c>UK_EXPENSE_CODE</c> on
/// <c>EXPENCECODE, VAHEDCODE</c>), mapped centrally by <c>UnitOfWork.SaveChangesAsync</c> to
/// <c>DuplicateKeyException</c> → 409.
///
/// <b>400 also covers two mapped FK violations</b>: an <c>AccountCodeId</c>/<c>ExpenseGroupId</c>
/// that does not reference an existing row violates <c>FK_EXPENSE_ACCOUNTCODE</c>/
/// <c>FK_EXPENCEGROUP</c> and is mapped centrally to <c>ForeignKeyViolationException</c> → 400
/// (plain <see cref="ProblemDetails"/>, no <c>errors</c> dictionary).
///
/// ⚠️ Its child <c>TB_EXPENCE_LINK_TAFSILI</c> is permanently embedded per team rule — no
/// repository, no cascade, and no independent write path for it exists anywhere in this project.
///
/// <b><see cref="Create"/>/<see cref="Update"/>/<see cref="GetList"/> also declare <c>403
/// Forbidden</c></b> — <c>CreateExpenseCommand</c>/<c>UpdateExpenseCommand</c>/
/// <c>GetExpensesQuery</c> all implement <c>IVahedScopedCommand</c>/<c>IVahedScopedQuery</c>, so
/// <c>VahedScopeBehavior</c> throws <c>MissingVahedScopeException</c> → 403 (via
/// <c>GlobalExceptionHandler</c>) when the authenticated caller has no usable unit-scope claim.
/// <see cref="GetById"/>/<see cref="Delete"/> do not opt in and never return 403 for this reason.
/// </summary>
[ApiController]
[Route("api/expenses")]
public sealed class ExpensesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExpensesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new expense-type row (<c>TB_EXPENCE</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateExpenseResponse(id));
    }

    /// <summary>
    /// Returns a page of expense-type rows.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetExpensesQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single expense-type row by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExpenseByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing expense-type row (<c>TB_EXPENCE</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateExpenseCommand(
            id,
            request.ExpenseCode,
            request.ExpenseName,
            request.Description,
            request.DefaultAmount,
            request.ExpenseGroupId,
            request.AccountCodeId);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateExpenseResponse(id));
    }

    /// <summary>
    /// Soft-deletes an expense-type row (<c>TB_EXPENCE.ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteExpenseCommandHandler"/> XML doc. Returns <b>200</b> with the affected
    /// <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteExpenseCommand(id), cancellationToken);

        return Ok(new DeleteExpenseResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="ExpensesController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_EXPENCE.ID</c>.</param>
public sealed record CreateExpenseResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="ExpensesController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_EXPENCE.ID</c> that was updated (from the route).</param>
public sealed record UpdateExpenseResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="ExpensesController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_EXPENCE.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteExpenseResponse(Guid Id);

/// <summary>
/// Request body for <see cref="ExpensesController.Update"/>. Mirrors every field of
/// <see cref="UpdateExpenseCommand"/> except <c>Id</c> (bound from the route instead) and
/// <c>VahedCode</c> (server-assigned by <c>VahedScopeBehavior</c> — see
/// <see cref="UpdateExpenseCommand.VahedCode"/> XML doc — so it is not part of this request body
/// at all, not even as an ignored field).
/// </summary>
public sealed record UpdateExpenseRequest(
    string ExpenseCode,
    string ExpenseName,
    string? Description,
    decimal? DefaultAmount,
    Guid? ExpenseGroupId,
    Guid? AccountCodeId);
