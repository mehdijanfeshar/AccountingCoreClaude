using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// The single place «کدام <see cref="TreasuryRole"/> مرحلهٔ فعلی یک درخواست پرداخت را تأیید
/// می‌کند؟» زندگی می‌کند — خزانه‌داری، بخش ۴-الف (<c>docs/tankhah-khazaneh-module.md</c> §۱۰).
/// Shared by <c>PaymentRequestApprovalService</c> (the authoritative gate) and
/// <c>GetApprovalCartableQueryHandler</c> (read-only <c>pendingForMe</c> display logic), so the two
/// can never drift apart.
/// </summary>
public static class PaymentRequestStageRoleMap
{
    /// <returns>The role required to approve/return/reject a request currently in
    /// <paramref name="state"/>, or <see langword="null"/> when <paramref name="state"/> is not a
    /// Pending* stage.</returns>
    public static TreasuryRole? RequiredRoleForState(PaymentRequestState state) => state switch
    {
        PaymentRequestState.PendingUnitManager => TreasuryRole.UnitManager,
        PaymentRequestState.PendingFinanceManager => TreasuryRole.FinanceManager,
        PaymentRequestState.PendingCeo => TreasuryRole.Ceo,
        _ => null,
    };
}
