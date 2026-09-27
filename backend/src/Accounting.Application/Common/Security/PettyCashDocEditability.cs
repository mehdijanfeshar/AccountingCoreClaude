using System.Linq;
using Accounting.Application.Common.Exceptions;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Security;

/// <summary>
/// The single place «کدام صورت‌هزینهٔ تنخواه قابل ویرایش/حذف است» lives, so the three write
/// paths (Update, Submit's source-state check, Delete) cannot drift apart — the same "one rule,
/// one home" shape as <c>VoucherEditability</c>.
///
/// <b>The rule</b> (<c>docs/tankhah-khazaneh-module.md</c> §4): only <see cref="PettyCashDocState.Draft"/>
/// and <see cref="PettyCashDocState.Returned"/> may be updated or submitted; only
/// <see cref="PettyCashDocState.Draft"/> may be deleted.
/// </summary>
public static class PettyCashDocEditability
{
    private static readonly PettyCashDocState[] EditableStates =
    {
        PettyCashDocState.Draft,
        PettyCashDocState.Returned,
    };

    public static bool IsEditable(PettyCashDocState state) => EditableStates.Contains(state);

    public static bool IsDeletable(PettyCashDocState state) => state == PettyCashDocState.Draft;

    /// <summary>Throws <see cref="PettyCashDocNotEditableException"/> unless
    /// <paramref name="state"/> is <see cref="PettyCashDocState.Draft"/> or
    /// <see cref="PettyCashDocState.Returned"/>. Used by both Update and Submit — both require
    /// the document to still be in one of these two states before they act.</summary>
    public static void EnsureEditable(Guid expenseDocId, PettyCashDocState state)
    {
        if (!IsEditable(state))
        {
            throw new PettyCashDocNotEditableException(expenseDocId, state, "updated");
        }
    }

    /// <summary>Throws <see cref="PettyCashDocNotEditableException"/> unless
    /// <paramref name="state"/> is <see cref="PettyCashDocState.Draft"/>.</summary>
    public static void EnsureDeletable(Guid expenseDocId, PettyCashDocState state)
    {
        if (!IsDeletable(state))
        {
            throw new PettyCashDocNotEditableException(expenseDocId, state, "deleted");
        }
    }
}
