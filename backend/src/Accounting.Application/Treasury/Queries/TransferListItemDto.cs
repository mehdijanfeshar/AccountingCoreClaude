using Accounting.Application.Common;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary>One row of <c>GET api/treasury/transfers</c>.</summary>
public sealed record TransferListItemDto(
    Guid Id,
    string Code,
    Guid SourceBankAccountId,
    Guid DestBankAccountId,
    decimal Amount,
    TransferState State,
    string TransferDate,
    DateTime CreatedDate,
    string AddUserId);

/// <summary><c>GET api/treasury/transfers</c> response — page + per-state row counts.</summary>
public sealed record TransferListResult(
    PagedResult<TransferListItemDto> Page,
    IReadOnlyList<TransferStateCountDto> StateCounts);

public sealed record TransferStateCountDto(TransferState State, int Count);
