using Accounting.Domain.Entity;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Write-side, insert-only repository for <see cref="TB_TR_TRANSFER_EVENT"/> — خزانه‌داری، بخش
/// ۴-ج. Same shape as <c>IPaymentRequestEventRepository</c>, minus the "last approve event" lookup
/// (انتقال وجه has only one approval stage, so there is no consecutive-approver rule to back).
/// </summary>
public interface ITreasuryTransferEventRepository
{
    Task AddAsync(TB_TR_TRANSFER_EVENT transferEvent, CancellationToken cancellationToken = default);
}
