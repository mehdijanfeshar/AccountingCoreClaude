namespace Accounting.Application.PettyCash.Commands.Common;

/// <summary>
/// The single place «آیا کاربر جاری مجاز به بررسی/تأیید/برگشت/رد این صورت‌هزینه است؟» lives, so
/// StartReview/Approve/Return/Reject/BulkApprove cannot drift apart — same "one rule, one home"
/// shape as <see cref="Accounting.Application.Common.Security.PettyCashDocEditability"/>.
///
/// <b>The rule</b> (<c>docs/tankhah-khazaneh-module.md</c>, تصمیم‌های بخش ۲، قاعدهٔ SoD):
/// (الف) the caller must have an active (<c>ISDELETED == false</c>) <c>TB_PC_REVIEWER</c> row for
/// the document's <c>FUND_ID</c>, otherwise <b>403</b>; (ب) the caller must not be the
/// document's own creator (<c>ADDUSERID</c>), otherwise <b>409</b> (segregation of duties, not a
/// permissions gap). Checked in that order.
/// </summary>
public interface IPettyCashReviewAuthorizer
{
    /// <summary>
    /// Throws <see cref="Accounting.Application.Common.Exceptions.PettyCashReviewerAccessDeniedException"/>
    /// or <see cref="Accounting.Application.Common.Exceptions.PettyCashSelfReviewConflictException"/>
    /// on the first rule that fails; does nothing when both pass.
    /// </summary>
    Task EnsureCanReviewAsync(
        Guid expenseDocId,
        Guid fundId,
        string documentCreatorUserId,
        CancellationToken cancellationToken = default);
}
