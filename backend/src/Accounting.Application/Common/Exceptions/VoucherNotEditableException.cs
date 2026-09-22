using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// An attempt to modify or delete a voucher whose <c>DOCLIFE</c> state has passed the point where
/// it may still be changed.
///
/// <para>
/// <b>The rule</b> (project owner, 2026-09-22): a voucher is editable and deletable only in
/// <see cref="DocLife.Draft"/> (یادداشت) and <see cref="DocLife.Temporary"/> (موقت). Once it
/// reaches <see cref="DocLife.Reviewed"/> (بررسی‌شده) or <see cref="DocLife.Accepted"/>
/// (تأیید دائم) it is view-only, and the way back is an explicit state change — the separate
/// <c>change-state</c> operation added in phase 30 — not a quiet edit.
/// </para>
///
/// <para>
/// <b>Why this exists server-side and not only in the cartable UI.</b> Team working-rule #1: a
/// business rule that lives only in the UI is not enforced. This is the same trap phase 35 had to
/// undo for «تفصیلی الزامی», which was UI-only from phase 22 and left the API open the whole time.
/// This closes the remaining half of open risk #5 for the delete/update paths.
/// </para>
///
/// <para>
/// <b>409, not 403.</b> 403 would say "you may never do this", which is wrong — the caller is
/// perfectly entitled to edit this voucher, just not while it is in this state, and they can fix
/// that themselves by moving it back. 409 Conflict is the accurate answer: the request conflicts
/// with the resource's current state.
/// </para>
/// </summary>
public sealed class VoucherNotEditableException : Exception
{
    public VoucherNotEditableException(Guid voucherHeadId, DocLife? docLife)
        : base($"Voucher {voucherHeadId} is in state {(docLife.HasValue ? docLife.Value.ToString() : "null")} and cannot be modified or deleted.")
    {
        VoucherHeadId = voucherHeadId;
        DocLife = docLife;
    }

    public Guid VoucherHeadId { get; }

    public DocLife? DocLife { get; }

    /// <summary>
    /// Names the state and the way out. Both are things the accountant already sees in the
    /// cartable, so nothing is disclosed — and without the second half the error is a dead end.
    /// </summary>
    public string PublicDetail =>
        "سند در وضعیت «" + DocLifeLabel(DocLife) + "» است و قابل ویرایش یا حذف نیست. " +
        "برای تغییر، ابتدا وضعیت آن را به «یادداشت» یا «موقت» برگردانید.";

    private static string DocLifeLabel(DocLife? docLife) => docLife switch
    {
        Accounting.Domain.ValueObjects.DocLife.Draft => "یادداشت",
        Accounting.Domain.ValueObjects.DocLife.Temporary => "موقت",
        Accounting.Domain.ValueObjects.DocLife.Reviewed => "بررسی‌شده",
        Accounting.Domain.ValueObjects.DocLife.Accepted => "تأیید دائم",
        _ => "بدون وضعیت",
    };
}
