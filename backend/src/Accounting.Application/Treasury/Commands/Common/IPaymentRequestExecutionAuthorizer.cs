namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// The role gate shared by <c>execute</c>/<c>suspend</c>/<c>resume</c> — خزانه‌داری، بخش ۴-ب (owner
/// decision ۲۰۲۶-۰۹-۲۹): only a caller holding an active
/// <see cref="Accounting.Domain.ValueObjects.TreasuryRole.Treasurer"/> role in the unit may act on
/// a درخواست پرداخت's execution. Deliberately separate from <c>ITreasuryRoleAuthorizer</c> (the
/// unit-wide settings/roles admin gate) — same reasoning as
/// <c>PaymentRequestApprovalService.EnsureHasRoleAsync</c> for the approval-chain stage roles: a
/// request-scoped failure here must carry the request-scoped
/// <see cref="Accounting.Application.Common.Exceptions.PaymentRequestTreasurerRoleRequiredException"/>,
/// not the differently-worded unit-wide one.
/// </summary>
public interface IPaymentRequestExecutionAuthorizer
{
    /// <exception cref="Accounting.Application.Common.Exceptions.PaymentRequestTreasurerRoleRequiredException" />
    Task EnsureTreasurerAsync(Guid paymentRequestId, string vahedCode, CancellationToken cancellationToken = default);
}
