namespace Accounting.Application.Common.Exceptions;

/// <summary>
/// Thrown by <c>Accounting.Application.PettyCash.Commands.Common.PettyCashReviewAuthorizer</c>
/// when the current caller has no active <c>TB_PC_REVIEWER</c> row for a صورت‌هزینه's
/// <c>FUND_ID</c> — i.e. they are not a designated بررسی‌کننده for that تنخواه at all, so
/// none of StartReview/Approve/Return/Reject is theirs to perform
/// (<c>docs/tankhah-khazaneh-module.md</c>, تصمیم‌های بخش ۲، قاعدهٔ SoD جزء (الف)).
///
/// <b>403, not 404 or 409</b> — this is a straight permissions gap, same shape as
/// <see cref="UnitAccessDeniedException"/>: the caller is authenticated and the document exists,
/// they simply are not on the reviewer list for its fund. Distinct from
/// <see cref="PettyCashSelfReviewConflictException"/> (409), which fires only once this check has
/// already passed.
/// </summary>
public sealed class PettyCashReviewerAccessDeniedException : Exception
{
    public PettyCashReviewerAccessDeniedException(Guid expenseDocId, Guid revolvingFundId)
        : base($"Caller is not an active reviewer for revolving fund {revolvingFundId} (expense document {expenseDocId}).")
    {
        ExpenseDocId = expenseDocId;
        RevolvingFundId = revolvingFundId;
    }

    public Guid ExpenseDocId { get; }

    public Guid RevolvingFundId { get; }

    public string PublicDetail => "شما بررسی‌کنندهٔ مجاز این تنخواه نیستید.";
}
