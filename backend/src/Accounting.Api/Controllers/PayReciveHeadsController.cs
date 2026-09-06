using Accounting.Application.Common;
using Accounting.Application.PayReciveHeads.Commands.CreatePayReciveHead;
using Accounting.Application.PayReciveHeads.Commands.DeletePayReciveHead;
using Accounting.Application.PayReciveHeads.Commands.UpdatePayReciveHead;
using Accounting.Application.PayReciveHeads.Queries;
using Accounting.Application.PayReciveHeads.Queries.GetPayReciveHeadById;
using Accounting.Application.PayReciveHeads.Queries.GetPayReciveHeads;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// Thin HTTP surface over the <c>TB_PAYRECIVHEAD</c> (Legacy payment/receipt document header —
/// سرسند دریافت و پرداخت) write and read use cases. Every action does nothing but: build a
/// request → send it through MediatR → map the result to an <see cref="IActionResult"/>. All
/// validation lives in FluentValidation validators (run by <c>ValidationBehavior</c>) and all
/// business rules live in the Application/Domain layers — never here.
///
/// ⚠️⚠️ <b>HEAD ONLY — this is deliberate, not an oversight.</b> <c>TB_PAYRECIVHEAD</c>'s child
/// table <c>TB_PAYRECIVDETAIL</c> is explicitly out of scope for this batch: the aggregate
/// boundary for this Head/Detail pair has NOT been decided (see <c>docs/open-decisions.md</c>).
/// This controller follows the phase-5..8 <c>VoucherHead</c> and phase-15 <c>ElamHead</c>
/// precedent — standalone Head CRUD only. There is no route, command, repository, or cascade
/// touching <c>TB_PAYRECIVDETAIL</c> anywhere in this batch. Do not add one without first
/// getting the aggregate-boundary decision from <c>team-lead</c>.
///
/// ⚠️ Note that the reference project treats <c>PayReciveDetail</c> as an <b>independent</b>
/// aggregate root with its own <c>Created/Update/DeleteDetailSingle</c> commands
/// (<c>docs/centralaccount-business-reference.md</c> §21-6) <em>and</em> creates the header
/// compositely with its rows in <c>AddPayReciveCommand</c> — i.e. exactly the "hybrid" shape the
/// project owner chose for vouchers in phase 10. Reaching that shape here needs the same
/// explicit decision; it was not assumed.
///
/// Every action below also implicitly returns <b>401 Unauthorized</b>: this controller has no
/// <c>[AllowAnonymous]</c>, so it falls under the API-wide fallback policy
/// (<c>SetFallbackPolicy(RequireAuthenticatedUser)</c> in <c>Program.cs</c>).
///
/// <b>No PUT/DELETE anywhere in this controller — by explicit project-owner mandate, not an
/// internal architecture choice.</b> Update/Delete are exposed as <c>POST</c> to
/// <c>{id}/update</c> and <c>{id}/delete</c>, mirroring <see cref="ElamHeadsController"/>.
///
/// <b>409 Conflict is deliberately NOT declared anywhere in this controller.</b>
/// <c>TB_PAYRECIVHEAD</c> has <b>no UNIQUE constraint at all</b> in <c>LegacyDbContext</c>, so
/// there is nothing for <c>UnitOfWork.SaveChangesAsync</c> to translate into a
/// <c>DuplicateKeyException</c>. Declaring 409 would be speculation — the same judgement already
/// applied to <c>TB_RECEIP</c> and <c>TB_CHEQUES_INCORRENT</c> in phase 15. ⚠️ The practical
/// consequence is that <b>two headers with the same <c>PayReciveCode</c> are accepted</b>; the
/// reference project blocks that in application code, and we deliberately did not re-create that
/// guard (see <see cref="CreatePayReciveHeadCommand"/> XML doc).
///
/// <b>400 also covers a mapped FK violation</b>: a <c>VoucherHeadId</c> that does not reference
/// an existing row violates <c>FK_PAYRECIV_VOCHERHEAD</c> — the table's only FK — and is mapped
/// centrally to <c>ForeignKeyViolationException</c> → 400 (plain <see cref="ProblemDetails"/>,
/// no <c>errors</c> dictionary — naming the offending field would mean leaking the Oracle
/// constraint name).
///
/// ⚠️⚠️ <b><c>PayReciveType</c> (<c>PAYRECIVTYPE</c>) is a CONFIRMED <c>bool?</c>-should-be-enum
/// column, flagged not fixed</b>: the reference project models it as a three-valued
/// <c>PayRecivType</c> (۱پرداخت ۲دریافت ۳همه), so the third value is unreachable through this
/// API. See <see cref="CreatePayReciveHeadCommand"/> XML doc for the full write-up. Re-typing it
/// is a breaking API-contract change and out of scope for this batch.
/// </summary>
[ApiController]
[Route("api/pay-recive-heads")]
public sealed class PayReciveHeadsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PayReciveHeadsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new payment/receipt header (<c>TB_PAYRECIVHEAD</c> row). HEAD ONLY — does not
    /// create any <c>TB_PAYRECIVDETAIL</c> row.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreatePayReciveHeadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePayReciveHeadCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new CreatePayReciveHeadResponse(id));
    }

    /// <summary>
    /// Returns a page of payment/receipt headers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PayReciveHeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetList(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPayReciveHeadsQuery(pageNumber, pageSize), cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns a single payment/receipt header by <c>ID</c>, or 404 when it does not exist.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PayReciveHeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPayReciveHeadByIdQuery(id), cancellationToken);

        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Fully replaces an existing payment/receipt header (<c>TB_PAYRECIVHEAD</c> row). Exposed as
    /// <c>POST {id}/update</c>, not <c>PUT</c> — by explicit project-owner mandate. <c>Id</c> is
    /// taken from the route, never the body. Returns <b>200</b> with the affected <c>Id</c> in
    /// the body (not 204), mirroring every other write action in this project. HEAD ONLY — does
    /// not touch any <c>TB_PAYRECIVDETAIL</c> row.
    /// </summary>
    [HttpPost("{id:guid}/update")]
    [ProducesResponseType(typeof(UpdatePayReciveHeadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdatePayReciveHeadRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePayReciveHeadCommand(
            id,
            request.PayReciveCode,
            request.PayReciveDate,
            request.PayReciveDescription,
            request.PayReciveType,
            request.VahedCode,
            request.Year,
            request.VoucherHeadId);

        await _mediator.Send(command, cancellationToken);

        return Ok(new UpdatePayReciveHeadResponse(id));
    }

    /// <summary>
    /// Soft-deletes a payment/receipt header (<c>TB_PAYRECIVHEAD.ISDELETED = true</c>). Exposed
    /// as <c>POST {id}/delete</c>, not <c>DELETE</c> — by explicit project-owner mandate.
    /// Idempotent: a row that is already soft-deleted still returns 200 — see
    /// <see cref="DeletePayReciveHeadCommandHandler"/> XML doc. Returns <b>200</b> with the
    /// affected <c>Id</c> in the body (not 204). HEAD ONLY — does NOT cascade to
    /// <c>TB_PAYRECIVDETAIL</c>, and does NOT block deletion of a document that has already been
    /// turned into an accounting voucher (a guard the reference project does have).
    /// </summary>
    [HttpPost("{id:guid}/delete")]
    [ProducesResponseType(typeof(DeletePayReciveHeadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePayReciveHeadCommand(id), cancellationToken);

        return Ok(new DeletePayReciveHeadResponse(id));
    }
}

/// <summary>
/// Response body for a successful <see cref="PayReciveHeadsController.Create"/> call.
/// </summary>
/// <param name="Id">The newly generated <c>TB_PAYRECIVHEAD.ID</c>.</param>
public sealed record CreatePayReciveHeadResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="PayReciveHeadsController.Update"/> call.
/// </summary>
/// <param name="Id">The <c>TB_PAYRECIVHEAD.ID</c> that was updated (from the route).</param>
public sealed record UpdatePayReciveHeadResponse(Guid Id);

/// <summary>
/// Response body for a successful <see cref="PayReciveHeadsController.Delete"/> call.
/// </summary>
/// <param name="Id">The <c>TB_PAYRECIVHEAD.ID</c> that was soft-deleted (from the route).</param>
public sealed record DeletePayReciveHeadResponse(Guid Id);

/// <summary>
/// Request body for <see cref="PayReciveHeadsController.Update"/>. Mirrors every field of
/// <see cref="UpdatePayReciveHeadCommand"/> except <c>Id</c>, which is bound from the route
/// instead.
/// </summary>
public sealed record UpdatePayReciveHeadRequest(
    string PayReciveCode,
    string PayReciveDate,
    string PayReciveDescription,
    bool? PayReciveType,
    string VahedCode,
    string Year,
    Guid? VoucherHeadId);
