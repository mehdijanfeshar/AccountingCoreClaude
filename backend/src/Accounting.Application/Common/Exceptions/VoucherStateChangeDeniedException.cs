using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// An attempt to move a voucher out of <see cref="DocLife.Accepted"/> (تأیید دائم).
///
/// <para>
/// <b>تأیید دائم is terminal</b> (project owner, 2026-09-22) — «دائم» is the whole meaning of the
/// state. Combined with phase 38's editability rule this is what makes the lifecycle actually
/// final: a permanently-approved voucher cannot be edited, cannot be deleted, and cannot be moved
/// back to a state where it could be. The only way to undo its effect is معکوس سند, which leaves
/// it in place and books the opposite entries beside it.
/// </para>
///
/// <para>
/// 409 rather than 403, for the same reason as <see cref="VoucherNotEditableException"/>: this is
/// about the resource's state, not the caller's permissions. Unlike that one, though, there is no
/// way out to offer — so the message does not pretend there is, and points at reversal instead.
/// </para>
/// </summary>
public sealed class VoucherStateChangeDeniedException : Exception
{
    public VoucherStateChangeDeniedException(Guid voucherHeadId)
        : base($"Voucher {voucherHeadId} is in DocLife.Accepted, which is terminal and cannot be changed.")
    {
        VoucherHeadId = voucherHeadId;
    }

    public Guid VoucherHeadId { get; }

    public string PublicDetail =>
        "سند در وضعیت «تأیید دائم» است و وضعیت آن قابل تغییر نیست. " +
        "برای خنثی‌کردن اثر این سند از «معکوس سند» استفاده کنید.";
}
