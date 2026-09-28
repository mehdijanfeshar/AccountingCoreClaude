using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Commands.Common;

/// <summary>
/// The single place the three plain بخش ۲ review transitions — StartReview, Return, Reject —
/// actually mutate a <see cref="TB_PC_EXPENSE_DOC"/>: load, authorize
/// (<see cref="IPettyCashReviewAuthorizer"/>), verify source state, mutate <c>DOC_STATE</c> (+
/// <c>RETURN_DEADLINE</c> for Return), mirror the Legacy head's <c>STATUS</c> via
/// <c>PettyCashStatusMap</c>, and stage one <see cref="TB_PC_DOC_EVENT"/> row. Same "one rule, one
/// home" shape as <c>IPettyCashSubmitRuleChecker</c>.
///
/// تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸، تأیید دومرحله‌ای) moved final approval — StartReview's old
/// <c>Approve</c> counterpart — out of this service and into
/// <see cref="IPettyCashFinalApprovalService"/>, because it needs extra steps this generic
/// shape does not fit (verified-check, amount-vs-authority, a second SoD check against the
/// verifier). <c>Verify</c> never touches <c>DOC_STATE</c> at all, so it lives entirely in
/// <c>VerifyPettyCashExpenseDocCommandHandler</c> instead.
///
/// <b>Never calls <see cref="Accounting.Application.Common.Interfaces.IUnitOfWork.SaveChangesAsync"/></b>
/// — only stages. The calling handler owns the transaction boundary.
/// </summary>
public interface IPettyCashReviewTransitionService
{
    /// <param name="expenseDocId">The document to transition.</param>
    /// <param name="vahedCode">Caller's unit — used for the ownership-checked load.</param>
    /// <param name="requiredFromState">The only <c>DOC_STATE</c> this action may start from;
    /// anything else throws <see cref="Accounting.Application.Common.Exceptions.PettyCashReviewStateConflictException"/>.</param>
    /// <param name="toState">The state to move the document to.</param>
    /// <param name="action">Recorded on the <see cref="TB_PC_DOC_EVENT"/> row.</param>
    /// <param name="allowedRoles">Passed straight through to
    /// <see cref="IPettyCashReviewAuthorizer.EnsureCanReviewAsync"/> — see that method's XML doc.
    /// StartReview passes <c>[Inspector]</c>; Return/Reject pass
    /// <c>[Inspector, FinanceManager, ChiefExecutive]</c> (تکمیل بخش ۲، ۲۰۲۶-۰۹-۲۸). Final approval
    /// no longer goes through this service — see <see cref="IPettyCashFinalApprovalService"/>.</param>
    /// <param name="note">Optional free-text note, recorded on the event row.</param>
    /// <param name="returnReasonsCsv">Comma-separated <see cref="PettyCashReturnReason"/> codes —
    /// only meaningful (and only ever non-null) for <see cref="PettyCashDocAction.Return"/>.</param>
    /// <param name="returnDeadline">Only meaningful for <see cref="PettyCashDocAction.Return"/> —
    /// stamped onto <c>TB_PC_EXPENSE_DOC.RETURN_DEADLINE</c>.</param>
    /// <returns>The change-tracked, now-mutated document.</returns>
    Task<TB_PC_EXPENSE_DOC> TransitionAsync(
        Guid expenseDocId,
        string vahedCode,
        PettyCashDocState requiredFromState,
        PettyCashDocState toState,
        PettyCashDocAction action,
        IReadOnlyCollection<PettyCashRole> allowedRoles,
        string? note,
        string? returnReasonsCsv,
        string? returnDeadline,
        CancellationToken cancellationToken = default);
}
