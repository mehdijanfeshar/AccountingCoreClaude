using Accounting.Application.PettyCash.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository backing <c>GET api/petty-cash/expense-docs/{id}/events</c>.
/// </summary>
public interface IPettyCashDocEventReadRepository
{
    /// <summary>
    /// Every event row for <paramref name="expenseDocId"/>, oldest first. Ownership of the parent
    /// document is verified against <paramref name="vahedCode"/> first (via
    /// <c>VahedOwnership</c>) — a missing parent document returns an empty list, since "no such
    /// document" is the caller's business to detect via <c>GetPettyCashExpenseDocById</c>, not
    /// this endpoint's.
    /// </summary>
    Task<IReadOnlyList<PettyCashDocEventDto>> GetByExpenseDocIdAsync(
        Guid expenseDocId,
        string vahedCode,
        CancellationToken cancellationToken = default);
}
