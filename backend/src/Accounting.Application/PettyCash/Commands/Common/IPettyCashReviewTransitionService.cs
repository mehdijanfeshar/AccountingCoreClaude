using Accounting.Domain.Entity;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Commands.Common;

/// <summary>
/// The single place a بخش ۲ review action (StartReview/Approve/Return/Reject) actually mutates a
/// <see cref="TB_PC_EXPENSE_DOC"/> — load, authorize (<see cref="IPettyCashReviewAuthorizer"/>),
/// verify source state, mutate <c>DOC_STATE</c> (+ <c>RETURN_DEADLINE</c> for Return), mirror the
/// Legacy head's <c>STATUS</c> via <c>PettyCashStatusMap</c>, and stage one
/// <see cref="TB_PC_DOC_EVENT"/> row — so the four single-document commands, and
/// <c>BulkApprovePettyCashExpenseDocsCommandHandler</c>'s per-id loop, share one implementation
/// instead of five near-identical copies. Same "one rule, one home" shape as
/// <c>IPettyCashSubmitRuleChecker</c>.
///
/// <b>Never calls <see cref="Accounting.Application.Common.Interfaces.IUnitOfWork.SaveChangesAsync"/></b>
/// — only stages. The calling handler owns the transaction boundary, which is what lets
/// <c>BulkApprovePettyCashExpenseDocsCommandHandler</c> call this once per id and still get
/// all-or-nothing semantics: if any call throws, the handler never reaches its own single
/// <c>SaveChangesAsync</c>, so nothing staged by earlier, individually-successful calls is
/// persisted either.
/// </summary>
public interface IPettyCashReviewTransitionService
{
    /// <param name="expenseDocId">The document to transition.</param>
    /// <param name="vahedCode">Caller's unit — used for the ownership-checked load.</param>
    /// <param name="requiredFromState">The only <c>DOC_STATE</c> this action may start from;
    /// anything else throws <see cref="Accounting.Application.Common.Exceptions.PettyCashReviewStateConflictException"/>.</param>
    /// <param name="toState">The state to move the document to.</param>
    /// <param name="action">Recorded on the <see cref="TB_PC_DOC_EVENT"/> row.</param>
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
        string? note,
        string? returnReasonsCsv,
        string? returnDeadline,
        CancellationToken cancellationToken = default);
}
