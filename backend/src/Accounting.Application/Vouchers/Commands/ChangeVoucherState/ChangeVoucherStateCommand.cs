using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Vouchers.Commands.ChangeVoucherState;

/// <summary>
/// Moves one or more vouchers to a new <see cref="DocLife"/> state (یادداشت / موقت / بررسی‌شده /
/// تأیید دائم). This is the کارتابل's "انتقال وضعیت" action.
///
/// <para>
/// <b>Batch by design, not for convenience.</b> The reference project's own
/// <c>ChangeStateCommand</c> takes a <c>List&lt;Guid&gt;</c>, because the کارتابل works by
/// selecting several rows and moving them together. One request, one transaction: either every
/// selected voucher moves or none does.
/// </para>
///
/// <para>
/// <b>Why this exists as its own command rather than a field on the update path.</b> Changing
/// what a voucher *says* and changing how final it *is* are different operations with different
/// meaning — the reference project separates them the same way (its
/// <c>UpdateVouchersHeadCommand</c> never touches <c>DOCLIFE</c>), and
/// <c>docs/centralaccount-business-reference.md</c> §۲۴ records that as a general pattern there,
/// not a voucher-specific quirk.
/// </para>
///
/// ⚠️ <b>There is deliberately NO transition guard, and that is a known open risk, not an
/// oversight.</b> Any state can move to any other, including تأیید دائم back to یادداشت. The
/// reference system does not guard it either — its <c>ChangeStateValidator</c> checks only
/// <c>IsInEnum</c> — so there is no established rule to port, and inventing one would be
/// fabricating business policy. This is the open half of risk #5; see
/// <c>docs/open-decisions.md</c>. What this command *does* fix is the other half: state changes
/// are now a distinct, auditable operation instead of an anonymous field write.
/// </summary>
/// <param name="VoucherHeadIds">The vouchers to move. Must be non-empty and free of duplicates.</param>
/// <param name="NewState">The state to move them all to.</param>
public sealed record ChangeVoucherStateCommand(
    IReadOnlyList<Guid> VoucherHeadIds,
    DocLife NewState) : IRequest;
