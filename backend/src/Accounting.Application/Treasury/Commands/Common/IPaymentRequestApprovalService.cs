using Accounting.Domain.Entity;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// The single place a درخواست پرداخت's approval-chain transitions happen — خزانه‌داری، بخش ۴-الف
/// (<c>docs/tankhah-khazaneh-module.md</c> §۱۰). Shared by <c>ApprovePaymentRequestCommandHandler</c>,
/// <c>BulkApprovePaymentRequestsCommandHandler</c>, <c>ReturnPaymentRequestCommandHandler</c> and
/// <c>RejectPaymentRequestCommandHandler</c> so the state machine, the role gate and both SoD
/// checks live in exactly one place — same shape as <c>IPettyCashFinalApprovalService</c>.
/// </summary>
public interface IPaymentRequestApprovalService
{
    /// <summary>
    /// Moves <paramref name="id"/> from its current Pending* stage to the next one (or to
    /// <see cref="Accounting.Domain.ValueObjects.PaymentRequestState.ReadyForExecution"/>), after
    /// the role gate and both SoD checks (creator conflict, consecutive-approver conflict). When
    /// <paramref name="isBulkApprove"/> is <see langword="true"/>, additionally refuses (before
    /// touching anything) a request whose <c>NET_PAYABLE_AMOUNT</c> exceeds the unit's
    /// <c>BULK_APPROVE_LIMIT</c> — only the single-request Approve endpoint may approve such a
    /// request.
    /// </summary>
    Task<TB_TR_PAYMENT_REQUEST> ApproveAsync(
        Guid id, string vahedCode, string? note, bool isBulkApprove, CancellationToken cancellationToken = default);

    /// <summary>Moves <paramref name="id"/> from any Pending* stage to
    /// <see cref="Accounting.Domain.ValueObjects.PaymentRequestState.Returned"/>. <paramref name="reason"/>
    /// is mandatory (enforced by the command's own FluentValidation rule, not here).</summary>
    Task ReturnAsync(Guid id, string vahedCode, string reason, CancellationToken cancellationToken = default);

    /// <summary>Moves <paramref name="id"/> from any Pending* stage to
    /// <see cref="Accounting.Domain.ValueObjects.PaymentRequestState.Rejected"/> (terminal).
    /// <paramref name="reason"/> is mandatory (enforced by the command's own FluentValidation rule,
    /// not here).</summary>
    Task RejectAsync(Guid id, string vahedCode, string reason, CancellationToken cancellationToken = default);
}
