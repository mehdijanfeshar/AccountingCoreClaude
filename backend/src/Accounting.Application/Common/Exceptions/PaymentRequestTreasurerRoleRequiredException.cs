namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when the caller holds no active <see cref="Accounting.Domain.ValueObjects.TreasuryRole.Treasurer"/>
/// role in the unit and attempts <c>execute</c>/<c>suspend</c>/<c>resume</c> — خزانه‌داری، بخش ۴-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). <b>403, not 409</b> — a straight permissions gap,
/// distinct wording from <c>PaymentRequestRoleRequiredException</c> (approval-chain stage roles).
/// </summary>
public sealed class PaymentRequestTreasurerRoleRequiredException : Exception
{
    public PaymentRequestTreasurerRoleRequiredException(Guid paymentRequestId)
        : base($"Caller holds no Treasurer role required to act on payment request {paymentRequestId}'s execution.")
    {
        PaymentRequestId = paymentRequestId;
    }

    public Guid PaymentRequestId { get; }

    public string PublicDetail => "فقط خزانه‌دار می‌تواند این عملیات را انجام دهد.";
}
