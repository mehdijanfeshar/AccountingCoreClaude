using Accounting.Application.Reports.AttributeAccountReconciliation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>
/// مغایرت‌گیری حساب‌های شناسه‌دار (FINACC-523) — سه سطح Drill-down: معین ← شناسه ← ردیف سند.
/// فقط <c>GET</c>؛ واحد از توکن/هدر <c>X-Vahed-Code</c> (<c>VahedScopeBehavior</c>)، نه از query.
/// </summary>
[ApiController]
[Route("api/reports/attribute-accounts")]
public sealed class AttributeAccountReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AttributeAccountReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>سطح ۱ — معین‌های شناسه‌دار با گردش و شمار شناسه‌های مغایر.</summary>
    [HttpGet("moeins")]
    [ProducesResponseType(typeof(IReadOnlyList<AttributeAccountMoeinDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMoeins(
        [FromQuery] string year = "",
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] int? docLife = null,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new GetAttributeAccountMoeinsQuery(year, fromDate, toDate, docLife), cancellationToken));

    /// <summary>سطح ۲ — شناسه‌های یک معین؛ مغایرها اول.</summary>
    [HttpGet("values")]
    [ProducesResponseType(typeof(IReadOnlyList<AttributeAccountValueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetValues(
        [FromQuery] Guid accountId,
        [FromQuery] string year = "",
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] int? docLife = null,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(
            new GetAttributeAccountValuesQuery(year, accountId, fromDate, toDate, docLife), cancellationToken));

    /// <summary>سطح ۳ — ردیف‌های سند یک شناسه (یا ردیف‌های بی‌شناسه با <c>withoutIdentifier=true</c>).</summary>
    [HttpGet("lines")]
    [ProducesResponseType(typeof(IReadOnlyList<AttributeAccountLineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLines(
        [FromQuery] Guid accountId,
        [FromQuery] string? attributeValue = null,
        [FromQuery] bool withoutIdentifier = false,
        [FromQuery] string year = "",
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        [FromQuery] int? docLife = null,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(
            new GetAttributeAccountLinesQuery(year, accountId, attributeValue, withoutIdentifier, fromDate, toDate, docLife),
            cancellationToken));
}
