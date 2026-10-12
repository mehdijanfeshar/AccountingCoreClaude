using Accounting.Application.AuditLogs;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

/// <summary>لاگ تغییرات — فقط خواندن و فقط نقش‌هایی که منوی «مشاهده لاگ تغییرات» را دارند (پیش‌فرض FINANCIAL CORE MALI ADMIN). هیچ endpoint نوشتنی ندارد.</summary>
[ApiController]
[Route("api/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search(
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] string? userId,
        [FromQuery] string? tableName,
        [FromQuery] string? actionType,
        [FromQuery] Guid? recordId,
        [FromQuery] Guid? operationId,
        [FromQuery] string? search,
        [FromQuery] ReportUnitScopeMode unitScope = ReportUnitScopeMode.Self,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(
            new GetAuditLogsQuery(
                fromDate?.ToUniversalTime(), toDate?.ToUniversalTime(), userId, tableName, actionType, recordId, operationId, search,
                pageNumber, pageSize) { UnitScope = unitScope },
            cancellationToken));

    [HttpGet("tables")]
    [ProducesResponseType(typeof(IReadOnlyList<AuditTableDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Tables(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetAuditTablesQuery(), cancellationToken));
}
