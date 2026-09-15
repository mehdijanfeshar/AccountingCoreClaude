using MediatR;

namespace Accounting.Application.Receipts.Commands.DeleteReceipt;

/// <summary>
/// Soft-deletes a <c>TB_RECEIP</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md).
///
/// ⚠️ Three other tables point AT this one (<c>TB_BANKCARTDETAIL</c>, <c>TB_PAYRECIVDETAIL</c>,
/// <c>TB_VOUCHERSDETAIL</c>) and none of them has any FK on the receipt-referencing column
/// (see <c>CreateReceiptCommand</c> XML doc), so soft-deleting a receipt does NOT cascade and
/// leaves any referencing rows active and silent. No cascade is implemented here.
/// </summary>
/// <param name="Id">The <c>TB_RECEIP.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteReceiptCommand(Guid Id) : IRequest;
