using MediatR;

namespace Accounting.Application.Vouchers.Commands.DeleteVoucherDetail;

/// <summary>
/// Soft-deletes a <c>TB_VOUCHERSDETAIL</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns, AND cascades the soft-delete to its own
/// <c>TB_VOUCHERDETAIL_LINK_TAFSILI</c> rows in the same call — see
/// <c>DeleteVoucherDetailCommandHandler</c> XML doc. Never issues a physical DELETE, matching
/// <c>DeleteVoucherHeadCommand</c>'s rationale (Legacy referential integrity + read side already
/// filters on <c>ISDELETED != true</c>).
///
/// This is independent of, and does NOT replace, the phase-9 cascade
/// (<see cref="Accounting.Application.Common.Interfaces.IVoucherHeadRepository.SoftDeleteDetailTreeAsync"/>)
/// that runs when the PARENT head is deleted — that cascade remains untouched and still soft-deletes
/// every detail line (and their tafsili links) under a deleted head in one call. This command is
/// for deleting ONE detail line on its own, independent of its head's lifecycle.
/// </summary>
/// <param name="Id">The <c>TB_VOUCHERSDETAIL.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteVoucherDetailCommand(Guid Id) : IRequest;
