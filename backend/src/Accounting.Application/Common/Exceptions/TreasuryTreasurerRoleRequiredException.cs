namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when the caller holds no active <see cref="Accounting.Domain.ValueObjects.TreasuryRole.Treasurer"/>
/// role in the unit and attempts دریافت وجه <c>register</c> or انتقال وجه <c>approve</c> —
/// خزانه‌داری، بخش ۴-ج (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). <b>403, not 409</b> — a
/// straight permissions gap. Deliberately generic (no receipt/transfer id) so both bخش-۴-ج write
/// paths can share it, unlike بخش ۴-ب's request-scoped
/// <c>PaymentRequestTreasurerRoleRequiredException</c>.
/// </summary>
public sealed class TreasuryTreasurerRoleRequiredException : Exception
{
    public TreasuryTreasurerRoleRequiredException()
        : base("Caller holds no Treasurer role required for this action.")
    {
    }

    public string PublicDetail => "فقط خزانه‌دار می‌تواند این عملیات را انجام دهد.";
}
