using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.BillLogs.Commands.DeleteBillLog;

/// <summary>
/// Soft-deletes a <c>TB_BILL_LOG</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE.
/// </summary>
/// <param name="Id">The <c>TB_BILL_LOG.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteBillLogCommand(Guid Id) : IRequest, IVahedScopedCommand
{
    /// <summary>
    /// Caller's own organizational unit, server-assigned by <c>VahedScopeBehavior</c> — never
    /// client input. Used to refuse a row belonging to another unit; see
    /// <c>VahedOwnership</c> and IDOR risk #1.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
