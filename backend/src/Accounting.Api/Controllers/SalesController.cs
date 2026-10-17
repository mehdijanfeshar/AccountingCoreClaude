using Accounting.Application.Sales;
using Accounting.Domain.Sales;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// ماژول فروش، فاز ۱ (DDL 080؛ طرح docs/sales-module.md). واحد فقط از هدر <c>X-Vahed-Code</c>؛ <c>year</c> از query.
/// نقش فروش هر کار در Handler کنترل می‌شود (<see cref="SalesAccess"/>). خطای ورودی/کنترل ⇒ 422 با <c>errors</c> و <c>checks</c>.
/// </summary>
[ApiController]
[Route("api/sales")]
public sealed class SalesController : ControllerBase
{
    private readonly IMediator _mediator;
    public SalesController(IMediator mediator) => _mediator = mediator;

    // ─────────── تنظیمات و اطلاعات پایه ───────────

    [HttpGet("setup")]
    public async Task<IActionResult> Setup(CancellationToken ct) => Ok(await _mediator.Send(new GetSalesSetupQuery(), ct));

    [HttpPost("setup")]
    public async Task<IActionResult> SaveSetup([FromBody] SaveSalesSetupCommand cmd, CancellationToken ct) => Write(await _mediator.Send(cmd, ct));

    [HttpGet("vat-rates")]
    public async Task<IActionResult> VatRates(CancellationToken ct) => Ok(await _mediator.Send(new ListSalesVatRatesQuery(), ct));

    [HttpPost("vat-rates")]
    public async Task<IActionResult> SaveVatRates([FromBody] SaveSalesVatRatesCommand cmd, CancellationToken ct) => Write(await _mediator.Send(cmd, ct));

    [HttpGet("roles")]
    public async Task<IActionResult> Roles(CancellationToken ct) => Ok(await _mediator.Send(new ListSalesRolesQuery(), ct));

    [HttpPost("roles")]
    public async Task<IActionResult> AddRole([FromBody] AddSalesRoleCommand cmd, CancellationToken ct) => Write(await _mediator.Send(cmd, ct));

    [HttpPost("roles/{id:guid}/delete")]
    public async Task<IActionResult> RemoveRole(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new RemoveSalesRoleCommand { Id = id }, ct);
        return NoContent();
    }

    [HttpGet("items")]
    public async Task<IActionResult> Items([FromQuery] string? search, [FromQuery] bool activeOnly = false, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new ListSalesItemsQuery(search, activeOnly), ct));

    [HttpPost("items")]
    public async Task<IActionResult> CreateItem([FromBody] SaveSalesItemCommand cmd, CancellationToken ct) => Write(await _mediator.Send(cmd, ct));

    [HttpPost("items/{id:guid}/update")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] SaveSalesItemCommand cmd, CancellationToken ct)
    {
        cmd.Id = id;
        return Write(await _mediator.Send(cmd, ct));
    }

    [HttpGet("customers")]
    public async Task<IActionResult> Customers([FromQuery] string? search, CancellationToken ct) =>
        Ok(await _mediator.Send(new ListSalesCustomersQuery(search), ct));

    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer([FromBody] SaveSalesCustomerCommand cmd, CancellationToken ct) => Write(await _mediator.Send(cmd, ct));

    [HttpPost("customers/{id:guid}/update")]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] SaveSalesCustomerCommand cmd, CancellationToken ct)
    {
        cmd.Id = id;
        return Write(await _mediator.Send(cmd, ct));
    }

    // ─────────── اسناد ───────────

    [HttpGet("docs")]
    public async Task<IActionResult> Docs(
        [FromQuery] SalesDocType? type, [FromQuery] SalesDocStatus? status, [FromQuery] Guid? customerId, [FromQuery] string? search,
        [FromQuery] string? fromDate, [FromQuery] string? toDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new ListSalesDocsQuery(type, status, customerId, search, fromDate, toDate, page, pageSize), ct));

    [HttpGet("docs/{id:guid}")]
    public async Task<IActionResult> Doc(Guid id, [FromQuery] string? year, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetSalesDocQuery(id) { Year = year }, ct));

    [HttpPost("docs/preview")]
    public async Task<IActionResult> Preview([FromBody] PreviewSalesDocQuery query, [FromQuery] string? year, CancellationToken ct)
    {
        query.Year = year;
        return Ok(await _mediator.Send(query, ct));
    }

    [HttpGet("docs/{id:guid}/convert")]
    public async Task<IActionResult> Conversion(Guid id, [FromQuery] SalesDocType target, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetSalesConversionQuery(id, target), ct));

    [HttpPost("docs")]
    public async Task<IActionResult> Create([FromBody] SaveSalesDocCommand cmd, [FromQuery] string? year, CancellationToken ct)
    {
        cmd.Year = year;
        return Doc(await _mediator.Send(cmd, ct));
    }

    [HttpPost("docs/{id:guid}/update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveSalesDocCommand cmd, [FromQuery] string? year, CancellationToken ct)
    {
        cmd.Id = id;
        cmd.Year = year;
        return Doc(await _mediator.Send(cmd, ct));
    }

    [HttpPost("docs/{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, [FromQuery] string? year, CancellationToken ct) =>
        Doc(await _mediator.Send(new SubmitSalesDocCommand { Id = id, Year = year }, ct));

    [HttpPost("docs/{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromQuery] string? year, CancellationToken ct) =>
        Doc(await _mediator.Send(new ApproveSalesDocCommand { Id = id, Year = year }, ct));

    [HttpPost("docs/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectSalesDocCommand cmd, CancellationToken ct)
    {
        cmd.Id = id;
        return Doc(await _mediator.Send(cmd, ct));
    }

    [HttpPost("docs/{id:guid}/post")]
    public async Task<IActionResult> Post(Guid id, [FromQuery] string? year, CancellationToken ct) =>
        Doc(await _mediator.Send(new PostSalesDocCommand { Id = id, Year = year }, ct));

    [HttpPost("docs/{id:guid}/void")]
    public async Task<IActionResult> Void(Guid id, CancellationToken ct) =>
        Doc(await _mediator.Send(new VoidSalesDocCommand { Id = id }, ct));

    private IActionResult Write(SalesWriteResult r) => r.Success ? Ok(r) : Problem422("اطلاعات ذخیره نشد.", r.Errors, null);

    private IActionResult Doc(SalesDocResult r) => r.Success ? Ok(r) : Problem422("عملیات سند فروش انجام نشد.", r.Errors, r.Checks);

    private ObjectResult Problem422(string title, IReadOnlyList<string> errors, IReadOnlyList<SalesCheck>? checks)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = title,
            Detail = string.Join(" · ", errors),
            Instance = HttpContext.Request.Path,
        };
        problem.Extensions["errors"] = errors;
        if (checks is not null) problem.Extensions["checks"] = checks;
        return new ObjectResult(problem) { StatusCode = problem.Status };
    }
}
