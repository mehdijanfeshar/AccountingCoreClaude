using Accounting.Domain.ValueObjects;

namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// One row of <c>GET api/petty-cash/expense-docs/{id}/events</c> — the «گردش عملیات» audit
/// trail, per <c>docs/tankhah-khazaneh-module.md</c> §5.
/// </summary>
public sealed record PettyCashDocEventDto(
    Guid Id,
    PettyCashDocAction Action,
    PettyCashDocState? FromState,
    PettyCashDocState? ToState,
    string? Note,
    string? ReturnReasons,
    string UserId,
    DateTime CreatedDate,
    string? ClientIp);
