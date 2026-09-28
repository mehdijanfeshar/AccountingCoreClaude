using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Commands.Common;

/// <summary>
/// The single place «آیا کاربر جاری مجاز به بررسی/کنترل/تأیید/برگشت/رد این صورت‌هزینه است؟» lives,
/// so StartReview/Verify/FinalApprove/Return/Reject/BulkApprove cannot drift apart — same "one
/// rule, one home" shape as <see cref="Accounting.Application.Common.Security.PettyCashDocEditability"/>.
///
/// <b>The rule</b> (<c>docs/tankhah-khazaneh-module.md</c>، تصمیم‌های بخش ۲ + تکمیل بخش ۲
/// ۲۰۲۶-۰۹-۲۸): (الف) the caller must hold at least one active (<c>ISDELETED == false</c>)
/// <c>TB_PC_REVIEWER</c> row for the document's <c>FUND_ID</c> — otherwise <b>403</b>؛ (ب) at
/// least one of those active roles must be in <paramref name="allowedRoles"/> — otherwise the same
/// <b>403</b> (a caller who is, say, only an <see cref="PettyCashRole.Inspector"/> is not a valid
/// reviewer *for an action that requires* <see cref="PettyCashRole.FinanceManager"/>); (پ) the
/// caller must not be the document's own creator (<c>ADDUSERID</c>) — otherwise <b>409</b>
/// (segregation of duties, not a permissions gap). Checked in that order.
/// </summary>
public interface IPettyCashReviewAuthorizer
{
    /// <param name="allowedRoles">Every role that may perform this specific action — e.g.
    /// <c>[Inspector]</c> for StartReview/Verify, or
    /// <c>[Inspector, FinanceManager, ChiefExecutive]</c> for Return/Reject.</param>
    /// <returns>The subset of the caller's active roles that is also in
    /// <paramref name="allowedRoles"/> — always non-empty when this method returns without
    /// throwing. Callers that need finer-grained authority logic beyond plain membership (e.g.
    /// final approval's amount-vs-<c>FINANCE_MANAGER_APPROVAL_LIMIT</c> check) inspect this set
    /// themselves rather than this method growing amount-awareness of its own.</returns>
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashReviewerAccessDeniedException">
    /// Thrown when the caller has no active reviewer role for this fund at all, or none of the
    /// roles it does hold is in <paramref name="allowedRoles"/>.</exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.PettyCashSelfReviewConflictException">
    /// Thrown when the caller is the document's own creator.</exception>
    Task<IReadOnlyCollection<PettyCashRole>> EnsureCanReviewAsync(
        Guid expenseDocId,
        Guid fundId,
        string documentCreatorUserId,
        IReadOnlyCollection<PettyCashRole> allowedRoles,
        CancellationToken cancellationToken = default);
}
