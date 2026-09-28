using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown when the caller has none of the roles required by a ترمیم action — بخش ۳-الف
/// (<c>docs/tankhah-khazaneh-module.md</c>). Unlike صورت‌هزینه review
/// (<c>PettyCashReviewerAccessDeniedException</c>), ترمیم authorization is a plain role check
/// against <c>TB_PC_REVIEWER.ROLE</c> for the fund — there is no equivalent "creator vs reviewer"
/// segregation-of-duties pre-check here, only the separate approver/payer conflict checks
/// (<see cref="PettyCashReplenishmentApproverConflictException"/>/
/// <see cref="PettyCashReplenishmentPayerConflictException"/>).
///
/// <b>403, not 409</b> — a straight permissions gap, same shape as
/// <c>PettyCashReviewerAccessDeniedException</c>.
/// </summary>
public sealed class PettyCashReplenishmentRoleRequiredException : Exception
{
    public PettyCashReplenishmentRoleRequiredException(Guid fundId, IReadOnlyCollection<PettyCashRole> requiredAnyOf)
        : base($"Caller holds none of the required roles ({string.Join(",", requiredAnyOf)}) for petty-cash fund {fundId}.")
    {
        FundId = fundId;
        RequiredAnyOf = requiredAnyOf;
    }

    public Guid FundId { get; }

    public IReadOnlyCollection<PettyCashRole> RequiredAnyOf { get; }

    public string PublicDetail => "شما نقش لازم برای این اقدام روی این تنخواه را ندارید.";
}
