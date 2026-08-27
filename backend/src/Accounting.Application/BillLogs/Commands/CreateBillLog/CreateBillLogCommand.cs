using MediatR;

namespace Accounting.Application.BillLogs.Commands.CreateBillLog;

/// <summary>
/// Creates a new <c>TB_BILL_LOG</c> row (a legacy bill/receipt log entry). Carries primitive
/// fields only — the handler is responsible for constructing the Domain entity. Returns the
/// newly generated <see cref="Guid"/> ID.
/// </summary>
/// <param name="LogDesc">LOG_DESC column (max 1000 chars, optional free-text description).</param>
/// <param name="LogDate">LOG_DATE column (max 8 chars — a Persian date string, kept as-is, not parsed).</param>
/// <param name="VahedCode">VAHEDCODE column (max 4 chars, required organizational unit code).</param>
/// <param name="Year">YEAR column (max 4 chars, fixed-length, required fiscal year).</param>
public sealed record CreateBillLogCommand(
    string? LogDesc,
    string? LogDate,
    string VahedCode,
    string Year) : IRequest<Guid>;
