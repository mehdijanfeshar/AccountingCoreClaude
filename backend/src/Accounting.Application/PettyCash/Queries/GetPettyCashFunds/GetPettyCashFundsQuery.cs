using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFunds;

/// <summary>
/// Returns every non-deleted <c>TB_PC_FUND</c> row belonging to the caller's unit, each with its
/// computed §2 balance summary — <c>GET api/petty-cash/funds</c>. Returns a bare array, not a
/// <see cref="Accounting.Application.Common.PagedResult{T}"/>, per the frontend contract
/// (<c>docs/tankhah-khazaneh-module.md</c> §5) and the same "small, unpaged list" reasoning as
/// <c>GetVahedTypesQuery</c>.
/// </summary>
public sealed record GetPettyCashFundsQuery : IRequest<IReadOnlyList<PettyCashFundDto>>, IVahedScopedQuery
{
    /// <summary>
    /// Server-assigned by <c>VahedScopeBehavior</c> — never client input. See
    /// <see cref="IVahedScopedQuery"/> for the full mechanism.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
