using Accounting.Application.Common;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary>One row of <c>GET api/treasury/receipts</c>.</summary>
public sealed record ReceiptListItemDto(
    Guid Id,
    string Code,
    string PayerName,
    decimal Amount,
    ReceiptState State,
    string ReceiptDate,
    DateTime CreatedDate,
    string AddUserId);

/// <summary><c>GET api/treasury/receipts</c> response — page + per-state row counts (unaffected by
/// the <c>state</c>/<c>search</c> filters, same shape as <see cref="PaymentRequestListResult"/>).</summary>
public sealed record ReceiptListResult(
    PagedResult<ReceiptListItemDto> Page,
    IReadOnlyList<ReceiptStateCountDto> StateCounts);

public sealed record ReceiptStateCountDto(ReceiptState State, int Count);
