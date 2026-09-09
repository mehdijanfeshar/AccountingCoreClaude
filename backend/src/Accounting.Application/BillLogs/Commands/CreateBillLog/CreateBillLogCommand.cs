using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.BillLogs.Commands.CreateBillLog;

/// <summary>
/// Creates a new <c>TB_BILL_LOG</c> row (a legacy bill/receipt log entry). Carries primitive
/// fields only — the handler is responsible for constructing the Domain entity. Returns the
/// newly generated <see cref="Guid"/> ID.
/// </summary>
/// <param name="LogDesc">LOG_DESC column (max 1000 chars, optional free-text description).</param>
/// <param name="LogDate">LOG_DATE column (max 8 chars — a Persian date string, kept as-is, not parsed).</param>
/// <param name="Year">YEAR column (max 4 chars, fixed-length, required fiscal year).</param>
public sealed record CreateBillLogCommand(
    string? LogDesc,
    string? LogDate,
    string Year) : IRequest<Guid>, IVahedScopedCommand
{
    /// <summary>
    /// VAHEDCODE column (max 4 chars, required organizational unit code). Never bound from the
    /// request body — <see cref="JsonIgnoreAttribute"/> keeps it out of both model binding and
    /// the Swagger schema — and never trusted even if a caller manages to set it:
    /// <c>VahedScopeBehavior</c> unconditionally overwrites this with the authenticated caller's
    /// own unit code before the request reaches <c>CreateBillLogCommandHandler</c>. See
    /// <see cref="IVahedScopedCommand"/> for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
