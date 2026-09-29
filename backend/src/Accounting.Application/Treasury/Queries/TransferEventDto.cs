using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary>One row of an انتقال وجه's «گردش عملیات» — same shape as <see cref="PaymentRequestEventDto"/>.</summary>
public sealed record TransferEventDto(
    Guid Id,
    TransferEventAction Action,
    TransferState? FromState,
    TransferState? ToState,
    string? Note,
    string UserId,
    DateTime CreatedDate,
    string? ClientIp);
