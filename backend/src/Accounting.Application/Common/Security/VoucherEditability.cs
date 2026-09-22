using Accounting.Application.Common.Exceptions;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Security;

/// <summary>
/// The single place the «کدام سند قابل ویرایش است» rule lives, so the five voucher write paths
/// (head update/delete, detail create/update/delete) cannot drift apart — the same "one rule, one
/// home" shape as <c>VahedOwnership</c>, and for the same reason: the reference project spread its
/// equivalent checks across handlers and reached 12 of 372.
/// </summary>
public static class VoucherEditability
{
    /// <summary>
    /// States in which a voucher may still be changed. Everything else is view-only.
    /// </summary>
    public static bool IsEditable(DocLife? docLife)
        => docLife is DocLife.Draft or DocLife.Temporary;

    /// <summary>
    /// Throws <see cref="VoucherNotEditableException"/> unless the voucher may still be changed.
    ///
    /// <para>
    /// ⚠️ <b>A null or out-of-enum <c>DOCLIFE</c> is treated as NOT editable.</b> The Oracle column
    /// carries <c>DEFAULT 0</c>, which is outside the enum and has no known meaning (see
    /// <see cref="DocLife"/>). Letting an unknown state fall through to "editable" would make the
    /// one value nobody can explain the most permissive one. Failing closed here is the safer
    /// default, and such a row can still be brought back through the explicit change-state
    /// operation.
    /// </para>
    /// </summary>
    public static void EnsureEditable(Guid voucherHeadId, DocLife? docLife)
    {
        if (!IsEditable(docLife))
        {
            throw new VoucherNotEditableException(voucherHeadId, docLife);
        }
    }
}
