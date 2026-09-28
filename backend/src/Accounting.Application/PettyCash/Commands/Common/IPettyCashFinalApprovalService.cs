using Accounting.Domain.Entity;

namespace Accounting.Application.PettyCash.Commands.Common;

/// <summary>
/// The single place final approval (<c>PendingReview</c> → <c>Approved</c>) actually happens —
/// تکمیل بخش ۲ (۲۰۲۶-۰۹-۲۸، تأیید دومرحله‌ای، صفحهٔ ۱۲ پاورپوینت). Used by both
/// <c>ApprovePettyCashExpenseDocCommandHandler</c> (single) and
/// <c>BulkApprovePettyCashExpenseDocsCommandHandler</c> (per id, all-or-nothing), so the rule
/// cannot drift apart — same "one rule, one home" shape as <see cref="IPettyCashReviewTransitionService"/>.
///
/// Deliberately separate from <see cref="IPettyCashReviewTransitionService"/> (the old, single-
/// stage <c>Approve</c> action reused it) — final approval now needs three checks that service's
/// generic shape does not fit: (۱) the document must already be verified
/// (<c>TB_PC_EXPENSE_DOC.VERIFIED_BY_USERID</c> not <see langword="null"/>); (۲) the caller must
/// not be the verifier (a second SoD rule, alongside "not the creator"); (۳) a caller whose only
/// qualifying role is <see cref="Accounting.Domain.ValueObjects.PettyCashRole.FinanceManager"/>
/// must not exceed <c>TB_PC_FUND.FINANCE_MANAGER_APPROVAL_LIMIT</c>.
///
/// <b>Never calls <see cref="Accounting.Application.Common.Interfaces.IUnitOfWork.SaveChangesAsync"/></b>
/// — only stages, exactly like <see cref="IPettyCashReviewTransitionService"/>, for the same
/// all-or-nothing reason.
/// </summary>
public interface IPettyCashFinalApprovalService
{
    /// <param name="expenseDocId">The document to finally approve.</param>
    /// <param name="vahedCode">Caller's unit — used for the ownership-checked loads.</param>
    /// <param name="note">Optional free-text note, recorded on the
    /// <see cref="Accounting.Domain.ValueObjects.PettyCashDocAction.FinalApprove"/> event row.</param>
    /// <returns>The change-tracked, now-<c>Approved</c> document.</returns>
    /// <exception cref="Accounting.Application.Common.Exceptions.NotFoundException">Document or its fund does not exist.</exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashReviewerAccessDeniedException">
    /// Caller has no active <see cref="Accounting.Domain.ValueObjects.PettyCashRole.FinanceManager"/>
    /// or <see cref="Accounting.Domain.ValueObjects.PettyCashRole.ChiefExecutive"/> role for the fund.</exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashSelfReviewConflictException">Caller created the document.</exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashReviewStateConflictException">Document is not <c>PendingReview</c>.</exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashNotVerifiedException">Document's control has not been verified yet.</exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashVerifierCannotApproveException">Caller is the document's own verifier.</exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashApprovalAuthorityExceededException">
    /// Caller only holds <see cref="Accounting.Domain.ValueObjects.PettyCashRole.FinanceManager"/> and the amount exceeds their limit.</exception>
    Task<TB_PC_EXPENSE_DOC> FinalApproveAsync(
        Guid expenseDocId,
        string vahedCode,
        string? note,
        CancellationToken cancellationToken = default);
}
