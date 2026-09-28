using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Commands.Common;

/// <summary>
/// The single place «آیا کاربر جاری نقش لازم روی این تنخواه برای این اقدام ترمیم را دارد؟» lives —
/// بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>). Deliberately simpler than
/// <see cref="IPettyCashReviewAuthorizer"/>: ترمیم has no "creator cannot also be the generic
/// reviewer" rule of its own — each action's own creator/approver conflict (approve vs creator,
/// record-payment vs approver) is checked separately by its own handler via
/// <see cref="Accounting.Application.Common.Exceptions.PettyCashReplenishmentApproverConflictException"/>/
/// <see cref="Accounting.Application.Common.Exceptions.PettyCashReplenishmentPayerConflictException"/>,
/// not by this authorizer.
/// </summary>
public interface IPettyCashReplenishmentAuthorizer
{
    /// <returns>The subset of the caller's active <c>TB_PC_REVIEWER</c> roles for
    /// <paramref name="fundId"/> that is also in <paramref name="allowedRoles"/> — always
    /// non-empty when this method returns without throwing.</returns>
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashReplenishmentRoleRequiredException">
    /// Thrown when the caller holds none of <paramref name="allowedRoles"/> as an active reviewer
    /// role for this fund.</exception>
    Task<IReadOnlyCollection<PettyCashRole>> EnsureHasRoleAsync(
        Guid fundId,
        IReadOnlyCollection<PettyCashRole> allowedRoles,
        CancellationToken cancellationToken = default);
}
