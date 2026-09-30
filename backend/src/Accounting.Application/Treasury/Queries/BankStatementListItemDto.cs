using Accounting.Application.Common;
using Accounting.Domain.ValueObjects;

namespace Accounting.Application.Treasury.Queries;

/// <summary>One row of <c>GET api/treasury/statements</c>.</summary>
public sealed record BankStatementListItemDto(
    Guid Id,
    string Code,
    Guid BankAccountId,
    string FromDate,
    string ToDate,
    decimal ClosingBalance,
    BankStatementSource Source,
    BankStatementState State,
    DateTime CreatedDate,
    string AddUserId);

/// <summary><c>GET api/treasury/statements</c> response — page + per-state row counts (unaffected
/// by the <c>state</c>/<c>bankAccountId</c> filters, same shape as every other treasury list).</summary>
public sealed record BankStatementListResult(
    PagedResult<BankStatementListItemDto> Page,
    IReadOnlyList<BankStatementStateCountDto> StateCounts);

public sealed record BankStatementStateCountDto(BankStatementState State, int Count);
