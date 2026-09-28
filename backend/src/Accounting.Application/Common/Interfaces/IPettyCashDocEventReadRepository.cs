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

    /// <summary>
    /// The most recent <see cref="Accounting.Domain.ValueObjects.PettyCashDocAction.Return"/>
    /// event for <paramref name="expenseDocId"/>, or <see langword="null"/> when there is none —
    /// backs <c>Accounting.Application.Common.Security.PettyCashReturnFieldPolicy</c>'s
    /// field-lock check (تکمیل بخش ۲، ۲۰۲۶-۰۹-۲۸). No ownership check here — callers already hold
    /// a change-tracked, ownership-verified parent document by this point.
    /// </summary>
    Task<PettyCashDocEventDto?> GetLastReturnEventAsync(
        Guid expenseDocId,
        CancellationToken cancellationToken = default);
}
