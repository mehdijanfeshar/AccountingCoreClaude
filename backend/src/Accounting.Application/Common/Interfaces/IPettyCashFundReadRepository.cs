using Accounting.Application.PettyCash.Queries;

namespace Accounting.Application.Common.Interfaces;

/// <summary>
/// Read-side repository backing <c>GET api/petty-cash/funds</c>. Deliberately NOT paged — per
/// the frontend contract, this endpoint returns a bare array — because the number of تنخواه
/// funds per unit is small (mirrors <c>GetRevolvingFundsQuery</c>'s own scope, just without the
/// pagination envelope this module's frontend does not want here).
/// </summary>
public interface IPettyCashFundReadRepository
{
    /// <summary>
    /// Every non-deleted <c>TB_REVOLVING_FUND</c> row belonging to <paramref name="vahedCode"/>,
    /// each joined to its (optional) <c>TB_PC_FUND_SETTING</c> and its computed §2 balance
    /// summary. Exact-equality unit filter only, same fail-closed rule as every other
    /// <c>IVahedScopedQuery</c> repository in this project.
    /// </summary>
    Task<IReadOnlyList<PettyCashFundDto>> GetAllAsync(string vahedCode, CancellationToken cancellationToken = default);
}
