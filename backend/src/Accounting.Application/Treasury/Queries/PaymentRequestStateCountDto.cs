using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary>Per-state row count, alongside the current page — same shape as
/// <c>PettyCashDocStateCountDto</c>.</summary>
public sealed record PaymentRequestStateCountDto(PaymentRequestState State, int Count);
