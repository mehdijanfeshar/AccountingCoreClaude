using Accounting.Application.PettyCash.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository backing <c>GET api/petty-cash/funds</c> and
/// <c>GET api/petty-cash/funds/{fundId}</c>. The list endpoint is deliberately NOT paged — per the
/// frontend contract, it returns a bare array — because the number of تنخواه funds per unit is
/// small.
/// </summary>
public interface IPettyCashFundReadRepository
{
    /// <summary>
    /// Every non-deleted <c>TB_PC_FUND</c> row belonging to <paramref name="vahedCode"/>, each with
    /// its computed §2 balance summary. Exact-equality unit filter only, same fail-closed rule as
    /// every other <c>IVahedScopedQuery</c> repository in this project.
    /// </summary>
    Task<IReadOnlyList<PettyCashFundDto>> GetAllAsync(string vahedCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the <c>TB_PC_FUND</c> row with the given <paramref name="id"/> regardless of its
    /// logical-delete state, or <see langword="null"/> if no such row exists (or belongs to another
    /// unit, in which case <c>UnitAccessDeniedException</c> is thrown instead — same pattern as
    /// every other by-id read repository in this project).
    /// </summary>
    Task<PettyCashFundDto?> GetByIdAsync(Guid id, string vahedCode, CancellationToken cancellationToken = default);
}
