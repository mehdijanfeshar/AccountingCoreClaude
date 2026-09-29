using Accounting.Application.Common;

namespace Accounting.Application.Treasury.Queries;

/// <summary>
/// <c>GET api/treasury/payment-requests</c> response envelope: <c>{ page, stateCounts }</c> — same
/// shape as <c>PettyCashExpenseDocListResult</c>.
/// </summary>
public sealed record PaymentRequestListResult(
    PagedResult<PaymentRequestListItemDto> Page,
    IReadOnlyList<PaymentRequestStateCountDto> StateCounts);
