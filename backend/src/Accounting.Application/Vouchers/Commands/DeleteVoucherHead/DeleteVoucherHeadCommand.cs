using MediatR;

namespace Accounting.Application.Vouchers.Commands.DeleteVoucherHead;

/// <summary>
/// Soft-deletes a <c>TB_VOUCHERSHEAD</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE —
/// <c>TB_VOUCHERSHEAD</c> participates in Legacy referential integrity (voucher lines,
/// self-referencing parent head, etc.) and the read side already filters on
/// <c>ISDELETED != true</c>, so a physical delete would both break FK integrity and diverge
/// from the original Legacy application's behaviour.
/// </summary>
/// <param name="Id">The <c>TB_VOUCHERSHEAD.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteVoucherHeadCommand(Guid Id) : IRequest;
