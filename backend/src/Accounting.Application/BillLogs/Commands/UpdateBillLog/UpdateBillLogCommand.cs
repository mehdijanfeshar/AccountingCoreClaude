using MediatR;

namespace Accounting.Application.BillLogs.Commands.UpdateBillLog;

/// <summary>
/// Fully replaces the writable fields of an existing <c>TB_BILL_LOG</c> row (PUT semantics, not
/// PATCH) — mirrors <c>UpdateAccountCodeCommand</c>. Deliberately excludes <c>ID</c>,
/// <c>ADDUSERID</c>, <c>CREATEDDATE</c> and <c>ISDELETED</c>.
/// </summary>
/// <param name="Id">The <c>TB_BILL_LOG.ID</c> to update (bound from the route, never the body).</param>
/// <param name="LogDesc">LOG_DESC column (max 1000 chars, optional free-text description).</param>
/// <param name="LogDate">LOG_DATE column (max 8 chars — a Persian date string).</param>
/// <param name="VahedCode">VAHEDCODE column (max 4 chars, required organizational unit code).</param>
/// <param name="Year">YEAR column (max 4 chars, fixed-length, required fiscal year).</param>
public sealed record UpdateBillLogCommand(
    Guid Id,
    string? LogDesc,
    string? LogDate,
    string VahedCode,
    string Year) : IRequest;
