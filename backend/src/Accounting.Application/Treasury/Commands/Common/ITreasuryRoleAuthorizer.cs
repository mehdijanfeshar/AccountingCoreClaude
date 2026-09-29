using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// The single place «آیا کاربر جاری یکی از نقش‌های لازم روی TB_TR_ROLE این واحد را دارد؟» لیو —
/// خزانه‌داری، بخش ۴-الف (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Unit-wide (not scoped to one
/// درخواست پرداخت) — used by settings/roles admin endpoints and by
/// <see cref="IPaymentRequestApprovalService"/> for the per-stage role gate.
/// </summary>
public interface ITreasuryRoleAuthorizer
{
    /// <returns>The subset of the caller's active <c>TB_TR_ROLE</c> roles for
    /// <paramref name="vahedCode"/> that is also in <paramref name="allowedRoles"/> — always
    /// non-empty when this method returns without throwing.</returns>
    /// <exception cref="Accounting.Application.Common.Exceptions.TreasuryRoleRequiredException">
    /// Thrown when the caller holds none of <paramref name="allowedRoles"/> as an active role for
    /// this unit.</exception>
    Task<IReadOnlyCollection<TreasuryRole>> EnsureHasRoleAsync(
        string vahedCode,
        IReadOnlyCollection<TreasuryRole> allowedRoles,
        CancellationToken cancellationToken = default);
}
