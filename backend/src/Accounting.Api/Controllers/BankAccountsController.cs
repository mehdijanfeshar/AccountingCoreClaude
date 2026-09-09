using Accounting.Application.BankAccounts.Commands.CreateBankAccount;
using Accounting.Application.BankAccounts.Commands.DeleteBankAccount;
using Accounting.Application.BankAccounts.Commands.UpdateBankAccount;
using Accounting.Application.BankAccounts.Queries;
using Accounting.Application.BankAccounts.Queries.GetBankAccountById;
using Accounting.Application.BankAccounts.Queries.GetBankAccounts;
using Accounting.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_ACCOUNT</c> (Legacy bank account master) write and read use
/// cases. Every action does nothing but: build a request → send it through MediatR → map the
/// result to an <see cref="IActionResult"/>. All validation lives in FluentValidation validators
/// (run by <c>ValidationBehavior</c>) and all business rules live in the Application/Domain
/// layers — never here.
///
/// ⚠️ <b><c>TB_ACCOUNT</c> is a BANK account — it is unrelated to the chart-of-accounts node
/// <c>TB_ACCOUNTCODE</c></b> (which already owns the <c>Accounts</c>/<see cref="AccountCodesController"/>
/// naming). Routed at <c>api/bank-accounts</c> to avoid any ambiguity with
/// <see cref="AccountCodesController"/>.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b><see cref="Create"/>/<see cref="Update"/>/<see cref="GetList"/> also declare <c>403
/// Forbidden</c></b> — <c>CreateBankAccountCommand</c>/<c>UpdateBankAccountCommand</c>/
/// <c>GetBankAccountsQuery</c> all implement <c>IVahedScopedCommand</c>/<c>IVahedScopedQuery</c>,
/// so <c>VahedScopeBehavior</c> throws <c>MissingVahedScopeException</c> → 403 (via
/// <c>GlobalExceptionHandler</c>) when the authenticated caller has no usable unit-scope claim.
/// <see cref="GetById"/>/<see cref="Delete"/> do not opt in and never return 403 for this reason —
/// see <c>UpdateBankAccountCommand</c> XML doc for the explicit scope note on what closing this
/// still leaves open.
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update/Delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="WorkShopsController"/>.
///
/// <b>409 Conflict IS declared on <see cref="Create"/>/<see cref="Update"/></b> — <c>TB_ACCOUNT</c>
/// carries a real UNIQUE constraint (<c>UK_ACCOUNT_ACCOUNTCODE</c> on
/// <c>ACCOUNTCODE_ID, VAHEDCODE</c>), mapped centrally by <c>UnitOfWork.SaveChangesAsync</c> to
/// <c>DuplicateKeyException</c> → 409.
///
/// <b>400 also covers three mapped FK violations</b>: an <c>AccountCodeId</c>/<c>BankId</c>/
/// <c>BranchId</c> that does not reference an existing row violates
/// <c>FK_ACCOUNTCODE_ACCOUNT</c>/<c>FK_BANK_ACCOUNT</c>/<c>FK_BANKBRANCH_ACCOUNT</c> and is
/// mapped centrally to <c>ForeignKeyViolationException</c> → 400 (plain
/// <see cref="ProblemDetails"/>, no <c>errors</c> dictionary — naming the offending field would
/// mean leaking the Oracle constraint name).
///
/// ⚠️ <c>AccountTypeId</c> has NO FK at all in the Legacy schema (even though
/// <c>TB_ACCOUNT_TYPE</c> exists) — an invalid value is written silently and the 400 mapping
/// above does not help; see <c>CreateBankAccountCommand</c> XML doc. ⚠️ <c>CheckFile</c> (BLOB)
/// has no size limit enforced at this layer — an upload/request-size policy is an unmade
/// decision. ⚠️ The permanently-embedded child <c>TB_ACCOUNT_LINK_TAFSILI</c> table is untouched
/// by this controller entirely (per team rule, every <c>*_LINK_TAFSIL*</c> table stays embedded)
/// — soft-deleting a bank account here does NOT cascade to it.
/// </summary>
[ApiController]
[Route("api/bank-accounts")]
public sealed class BankAccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BankAccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new bank account (<c>TB_ACCOUNT</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateBankAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBankAccountCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateBankAccountResponse(id));
    }

    /// <summary>
    /// Returns a page of bank accounts.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BankAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetBankAccountsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single bank account by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BankAccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBankAccountByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing bank account (<c>TB_ACCOUNT</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateBankAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBankAccountRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBankAccountCommand(
            id,
            request.AccountNumber,
            request.AccountHolder,
            request.CardNumber,
            request.ShebaNumber,
            request.FirstAmount,
            request.BankId,
            request.BranchId,
            request.AccountTypeId,
            request.AccountCodeId,
            request.CheckFile,
            request.AccountOpeningDate);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateBankAccountResponse(id));
    }

    /// <summary>
    /// Soft-deletes a bank account (<c>TB_ACCOUNT.ISDELETED = true</c>). Exposed as
    /// <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteBankAccountCommandHandler"/> XML doc. Returns <b>200</b> with the
    /// affected <c>Id</c> in the body (not 204). Does NOT cascade to
    /// <c>TB_ACCOUNT_LINK_TAFSILI</c> — see the class-level ⚠️ note above.
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteBankAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteBankAccountCommand(id), cancellationToken);

        return Ok(new DeleteBankAccountResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="BankAccountsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_ACCOUNT.ID</c>.</param>
public sealed record CreateBankAccountResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="BankAccountsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNT.ID</c> that was updated (from the route).</param>
public sealed record UpdateBankAccountResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="BankAccountsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_ACCOUNT.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteBankAccountResponse(Guid Id);

/// <summary>
/// Request body for <see cref="BankAccountsController.Update"/>. Mirrors every field of
/// <see cref="UpdateBankAccountCommand"/> except <c>Id</c> (bound from the route instead) and
/// <c>VahedCode</c> (server-assigned by <c>VahedScopeBehavior</c> — see
/// <see cref="UpdateBankAccountCommand.VahedCode"/> XML doc — so it is not part of this request
/// body at all, not even as an ignored field).
/// </summary>
public sealed record UpdateBankAccountRequest(
    string AccountNumber,
    string AccountHolder,
    string? CardNumber,
    string? ShebaNumber,
    decimal? FirstAmount,
    Guid? BankId,
    Guid? BranchId,
    Guid? AccountTypeId,
    Guid? AccountCodeId,
    byte[]? CheckFile,
    string? AccountOpeningDate);
