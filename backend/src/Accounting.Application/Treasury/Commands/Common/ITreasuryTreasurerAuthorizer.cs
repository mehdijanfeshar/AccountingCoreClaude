namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Checks the caller holds an active <see cref="Accounting.Domain.ValueObjects.TreasuryRole.Treasurer"/>
/// role in the unit — خزانه‌داری، بخش ۴-ج (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Shared by
/// دریافت وجه <c>register</c> and انتقال وجه <c>approve</c>, unlike بخش-۴-ب's request-scoped
/// <c>IPaymentRequestExecutionAuthorizer.EnsureTreasurerAsync(paymentRequestId, ...)</c> — same
/// underlying <c>ITreasuryRoleRepository.GetActiveRolesAsync</c> call, just without a
/// resource-specific exception payload.
/// </summary>
public interface ITreasuryTreasurerAuthorizer
{
    /// <exception cref="Accounting.Application.Common.Exceptions.TreasuryTreasurerRoleRequiredException">
    /// The caller holds no active Treasurer role in <paramref name="vahedCode"/>.
    /// </exception>
    Task EnsureTreasurerAsync(string vahedCode, CancellationToken cancellationToken = default);
}
