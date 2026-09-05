using Accounting.Application.Common;
using Accounting.Application.BankCartDetails.Commands.CreateBankCartDetail;
using Accounting.Application.BankCartDetails.Commands.DeleteBankCartDetail;
using Accounting.Application.BankCartDetails.Commands.UpdateBankCartDetail;
using Accounting.Application.BankCartDetails.Queries;
using Accounting.Application.BankCartDetails.Queries.GetBankCartDetailById;
using Accounting.Application.BankCartDetails.Queries.GetBankCartDetails;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_BANKCARTDETAIL</c> (Legacy bank-card/cheque-or-receipt detail
/// line) write and read use cases. Every action does nothing but: build a request → send it
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
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="WorkShopsController"/>.
///
/// <b>409 Conflict IS declared on <see cref="Create"/>/<see cref="Update"/></b> —
/// <c>TB_BANKCARTDETAIL</c> carries a real (unusually wide, 11-column) UNIQUE constraint
/// (<c>AK_AK_BANKCARTDETAIL_BANKCART</c>), mapped centrally by
/// <c>UnitOfWork.SaveChangesAsync</c> to <c>DuplicateKeyException</c> → 409.
///
/// <b>400 also covers two mapped FK violations</b>: a <c>CheckId</c>/<c>ReceipId</c> that does not
/// reference an existing row violates <c>FK_BANKCART_CHECK</c>/<c>FK_BANKCART_RECEIP</c> and is
/// mapped centrally to <c>ForeignKeyViolationException</c> → 400 (plain
/// <see cref="ProblemDetails"/>, no <c>errors</c> dictionary).
///
/// ⚠️ <c>BankId</c>, <c>BranchId</c> and <c>CheckIncorrentId</c> have NO FK at all — an invalid
/// value for any of them is written silently; the central FK→400 mapping does not help there. See
/// <see cref="CreateBankCartDetailCommand"/> XML doc.
///
/// ⚠️ <c>CheckReceiptType</c> is writable and typed as <see cref="bool"/>? here, but is very
/// likely a multi-valued enum in disguise (CLAUDE.md phase 12 pattern) — see
/// <see cref="CreateBankCartDetailCommand"/> XML doc; the CLR type is deliberately left unchanged.
/// ⚠️ <c>Debtor</c>/<c>Creditor</c> are independent nullable amounts — no balance is enforced.
/// </summary>
[ApiController]
[Route("api/bank-cart-details")]
public sealed class BankCartDetailsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BankCartDetailsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new bank-card detail line (<c>TB_BANKCARTDETAIL</c> row).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateBankCartDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBankCartDetailCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreateBankCartDetailResponse(id));
    }

    /// <summary>
    /// Returns a page of bank-card detail lines.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BankCartDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetBankCartDetailsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single bank-card detail line by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BankCartDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetBankCartDetailByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing bank-card detail line (<c>TB_BANKCARTDETAIL</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdateBankCartDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBankCartDetailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBankCartDetailCommand(
            id,
            request.ReceipId,
            request.CheckId,
            request.BankId,
            request.BranchId,
            request.AccountNumber,
            request.Month,
            request.Cheqno,
            request.RecivDate,
            request.CheckReceiptType,
            request.Debtor,
            request.Creditor,
            request.VahedCode,
            request.Year,
            request.CheckIncorrentId);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdateBankCartDetailResponse(id));
    }

    /// <summary>
    /// Soft-deletes a bank-card detail line (<c>TB_BANKCARTDETAIL.ISDELETED = true</c>). Exposed
    /// as <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeleteBankCartDetailCommandHandler"/> XML doc. Returns <b>200</b> with the
    /// affected <c>Id</c> in the body (not 204).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeleteBankCartDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteBankCartDetailCommand(id), cancellationToken);

        return Ok(new DeleteBankCartDetailResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="BankCartDetailsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_BANKCARTDETAIL.ID</c>.</param>
public sealed record CreateBankCartDetailResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="BankCartDetailsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_BANKCARTDETAIL.ID</c> that was updated (from the route).</param>
public sealed record UpdateBankCartDetailResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="BankCartDetailsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_BANKCARTDETAIL.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeleteBankCartDetailResponse(Guid Id);

/// <summary>
/// Request body for <see cref="BankCartDetailsController.Update"/>. Mirrors every field of
/// <see cref="UpdateBankCartDetailCommand"/> except <c>Id</c>, which is bound from the route
/// instead.
/// </summary>
public sealed record UpdateBankCartDetailRequest(
    Guid? ReceipId,
    Guid? CheckId,
    Guid? BankId,
    Guid? BranchId,
    string? AccountNumber,
    string? Month,
    string? Cheqno,
    string? RecivDate,
    bool? CheckReceiptType,
    decimal? Debtor,
    decimal? Creditor,
    string? VahedCode,
    string? Year,
    Guid? CheckIncorrentId);
