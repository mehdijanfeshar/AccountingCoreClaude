using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when the caller holds none of the roles a خزانه‌داری admin action (settings
/// upsert/roles CRUD) requires — <c>docs/tankhah-khazaneh-module.md</c> §۱۰. Unlike
/// <see cref="PaymentRequestRoleRequiredException"/> this is not scoped to one درخواست پرداخت, it
/// is a unit-wide admin gate (فقط <see cref="TreasuryRole.FinanceManager"/> واحد، با استثنای
/// bootstrap برای اولین نقش). <b>403</b>.
/// </summary>
public sealed class TreasuryRoleRequiredException : Exception
{
    public TreasuryRoleRequiredException(string vahedCode, TreasuryRole requiredRole)
        : base($"Caller holds no {requiredRole} role for unit {vahedCode}.")
    {
        VahedCode = vahedCode;
        RequiredRole = requiredRole;
    }

    public string VahedCode { get; }

    public TreasuryRole RequiredRole { get; }

    public string PublicDetail => "شما نقش لازم برای این اقدام مدیریتی خزانه‌داری را ندارید.";
}
