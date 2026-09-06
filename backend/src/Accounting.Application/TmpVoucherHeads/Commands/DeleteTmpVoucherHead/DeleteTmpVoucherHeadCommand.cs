using MediatR;

namespace Accounting.Application.TmpVoucherHeads.Commands.DeleteTmpVoucherHead;

/// <summary>
/// Soft-deletes a <c>TB_TMP_VOUCHERHEAD</c> row: sets <c>ISDELETED = true</c> plus the
/// <c>CHANGEUSERID</c>/<c>UPDATEDDATE</c> audit columns. Never issues a physical DELETE — this
/// project never issues physical deletes on any entity (see CLAUDE.md).
///
/// ⚠️ HEAD ONLY — does NOT cascade to <c>TB_TMP_VOUCHERSDETAIL</c>. See
/// <c>Accounting.Api.Controllers.TmpVoucherHeadsController</c> XML doc.
/// </summary>
/// <param name="Id">The <c>TB_TMP_VOUCHERHEAD.ID</c> to soft-delete (bound from the route).</param>
public sealed record DeleteTmpVoucherHeadCommand(Guid Id) : IRequest;
