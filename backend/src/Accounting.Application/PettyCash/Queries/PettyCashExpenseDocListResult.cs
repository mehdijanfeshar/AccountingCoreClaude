using Accounting.Application.Common;

namespace Accounting.Application.PettyCash.Queries;

/// <summary>
/// <c>GET api/petty-cash/expense-docs</c> response envelope: <c>{ page, stateCounts }</c>, per
/// <c>docs/tankhah-khazaneh-module.md</c> §5. Deliberately not a bare
/// <see cref="PagedResult{T}"/> — the کارتابل tab badges need the per-state counts alongside the
/// current page, computed by the same repository call (see <see cref="PettyCashDocStateCountDto"/>).
/// </summary>
public sealed record PettyCashExpenseDocListResult(
    PagedResult<PettyCashExpenseDocListItemDto> Page,
    IReadOnlyList<PettyCashDocStateCountDto> StateCounts);
