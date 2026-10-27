using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when the caller holds none of the roles a خزانه‌داری admin action (settings
/// upsert/roles CRUD) requires — <c>docs/tankhah-khazaneh-module.md</c> §۱۰. Unlike
/// <see cref="PaymentRequestRoleRequiredException"/> this is not scoped to one درخواست پرداخت, it
/// is a unit-wide admin gate (فقط <see cref="TreasuryRole.FinanceManager"/> واحد، با استثنای
/// bootstrap برای اولین نقش). <b>403</b>.
/// </summary>
public sealed class TreasuryRoleRequiredException : Exception, IPublicProblemException
{
    public TreasuryRoleRequiredException(string vahedCode, TreasuryRole requiredRole)
        : base($"Caller holds no {requiredRole} role for unit {vahedCode}.")
    {
        VahedCode = vahedCode;
        RequiredRole = requiredRole;
    }

    public string VahedCode { get; }

    public TreasuryRole RequiredRole { get; }

    public int StatusCode => 403;

    public string PublicDetail =>
        $"برای این کار باید در همین واحد نقش خزانهٔ «{RoleTitle(RequiredRole)}» داشته باشید — از منوی «نقش‌های خزانه» تعریف می‌شود.";

    private static string RoleTitle(TreasuryRole role) => role switch
    {
        TreasuryRole.UnitManager => "مدیر واحد",
        TreasuryRole.FinanceManager => "مدیر مالی",
        TreasuryRole.Ceo => "مدیرعامل",
        TreasuryRole.SeniorAccountant => "حسابدار ارشد",
        TreasuryRole.Treasurer => "خزانه‌دار",
        _ => role.ToString(),
    };
}
