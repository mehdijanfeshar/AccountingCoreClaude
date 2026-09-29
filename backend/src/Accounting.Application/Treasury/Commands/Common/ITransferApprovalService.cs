using Accounting.Domain.Entity;

namespace Accounting.Application.Treasury.Commands.Common;

/// <summary>
/// Shared گذار logic for یک انتقال وجه once it is <see cref="Accounting.Domain.ValueObjects.TransferState.PendingTreasurer"/>
/// — خزانه‌داری، بخش ۴-ج. Same role in this module as <c>IPaymentRequestApprovalService</c> is to
/// بخش-۴-الف, but much simpler: a single approval stage (خزانه‌دار only, no
/// <c>PaymentRequestStageRoleMap</c> needed) and only one blocking-check pair (موجودی + سقف روزانه)
/// on <see cref="ApproveAsync"/>. Every method here loads, checks role + SoD (approver/returner/
/// rejecter ≠ creator — applied uniformly across all three, same posture as
/// <c>PaymentRequestApprovalService</c>), applies the transition and appends exactly one
/// <c>TB_TR_TRANSFER_EVENT</c> row — but never calls <c>SaveChangesAsync</c>; the caller (handler)
/// owns that, wrapping <see cref="ApproveAsync"/> in an explicit transaction (it also stages a GL
/// voucher).
/// </summary>
public interface ITransferApprovalService
{
    /// <exception cref="Accounting.Application.Common.Exceptions.NotFoundException"/>
    /// <exception cref="Accounting.Application.Common.Exceptions.TreasuryTransferStateConflictException">
    /// Not <see cref="Accounting.Domain.ValueObjects.TransferState.PendingTreasurer"/>.
    /// </exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.TreasuryTreasurerRoleRequiredException"/>
    /// <exception cref="Accounting.Application.Common.Exceptions.TreasuryTransferApproverConflictException">
    /// Caller is the transfer's own creator.
    /// </exception>
    /// <exception cref="Accounting.Application.Common.Exceptions.TreasuryTransferInsufficientBalanceException"/>
    /// <exception cref="Accounting.Application.Common.Exceptions.TreasuryTransferDailyLimitExceededException"/>
    Task<TB_TR_TRANSFER> ApproveAsync(
        Guid id, string vahedCode, string bankReference, CancellationToken cancellationToken = default);

    Task ReturnAsync(Guid id, string vahedCode, string reason, CancellationToken cancellationToken = default);

    Task RejectAsync(Guid id, string vahedCode, string reason, CancellationToken cancellationToken = default);
}
