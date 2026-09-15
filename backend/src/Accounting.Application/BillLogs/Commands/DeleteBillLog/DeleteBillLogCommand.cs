using MediatR;

namespace Accounting.Application.BillLogs.Commands.DeleteBillLog;

/// <summary>
/// Soft-deletes a <c>TB_BILL_LOG</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE.
/// </summary>
/// <param name="Id">The <c>TB_BILL_LOG.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteBillLogCommand(Guid Id) : IRequest;
