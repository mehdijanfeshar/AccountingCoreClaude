using Accounting.Application.PettyCash.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository backing <c>GET api/petty-cash/expense-docs</c> and
/// <c>GET api/petty-cash/expense-docs/{id}</c>.
/// </summary>
public interface IPettyCashExpenseDocReadRepository
{
    /// <summary>
    /// A page of non-deleted expense documents belonging to <paramref name="vahedCode"/>, plus
    /// the per-state counts described on <see cref="PettyCashExpenseDocListResult"/>. Default
    /// order is oldest-submitted-first (documents with no <c>SUBMITTED_DATE</c> — i.e. drafts —
    /// sort last, since they are not yet in anyone's queue).
    /// </summary>
    Task<PettyCashExpenseDocListResult> GetPagedAsync(
        int pageNumber,
        int pageSize,
        PettyCashExpenseDocFilter filter,
        string vahedCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The single-document view, or <see langword="null"/> when no such row exists. Ownership is
    /// verified the same way every other by-id read repository in this project does.
    /// </summary>
    Task<PettyCashExpenseDocDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);
}
