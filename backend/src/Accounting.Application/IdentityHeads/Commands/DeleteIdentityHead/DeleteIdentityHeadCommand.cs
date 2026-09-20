using MediatR;

namespace Accounting.Application.IdentityHeads.Commands.DeleteIdentityHead;

/// <summary>
/// Soft-deletes one شناسنامه and, with it, its fixed-value items. Idempotent: deleting an
/// already-deleted record succeeds and changes nothing.
///
/// ⚠️ <b>Cascade stops at the fix items — <c>TB_IDENTITYDETAIL</c> rows are left alone.</b> Those
/// belong to voucher lines, not to this record, and silently soft-deleting a voucher's data as a
/// side effect of tidying base data would be a far worse outcome than leaving them. This is the
/// deliberate opposite of the voucher cascade added in phase 9, for the deliberate reason that
/// the aggregate boundary is different. See <c>IdentityHeadsController</c>.
/// </summary>
public sealed record DeleteIdentityHeadCommand(Guid Id) : IRequest;
