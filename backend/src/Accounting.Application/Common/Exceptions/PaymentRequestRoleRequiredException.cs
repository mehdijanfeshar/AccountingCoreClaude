using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when the caller holds none of the roles required by a درخواست پرداخت's current approval
/// stage (approve/return/reject) — خزانه‌داری، بخش ۴-الف (<c>docs/tankhah-khazaneh-module.md</c>
/// §۱۰). <b>403, not 409</b> — a straight permissions gap.
/// </summary>
public sealed class PaymentRequestRoleRequiredException : Exception
{
    public PaymentRequestRoleRequiredException(Guid paymentRequestId, TreasuryRole requiredRole)
        : base($"Caller holds no {requiredRole} role required for payment request {paymentRequestId}'s current stage.")
    {
        PaymentRequestId = paymentRequestId;
        RequiredRole = requiredRole;
    }

    public Guid PaymentRequestId { get; }

    public TreasuryRole RequiredRole { get; }

    public string PublicDetail => "شما نقش لازم برای این مرحله از تأیید را ندارید.";
}
