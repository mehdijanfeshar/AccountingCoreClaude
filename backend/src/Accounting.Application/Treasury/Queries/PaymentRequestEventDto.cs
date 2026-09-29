using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary>One row of a درخواست پرداخت's «گردش عملیات» — <c>docs/tankhah-khazaneh-module.md</c> §۱۰.</summary>
public sealed record PaymentRequestEventDto(
    Guid Id,
    PaymentRequestEventAction Action,
    PaymentRequestState? FromState,
    PaymentRequestState? ToState,
    string? Note,
    string UserId,
    DateTime CreatedDate,
    string? ClientIp);
