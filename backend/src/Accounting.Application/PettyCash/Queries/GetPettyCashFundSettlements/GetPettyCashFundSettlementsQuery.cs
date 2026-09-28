using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.PettyCash.Queries;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundSettlements;

/// <summary>
/// <c>GET api/petty-cash/funds/{fundId}/settlements</c> — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9). Every finalized period, newest-first — same
/// bare-array, always-200 shape as <c>GetPettyCashRefundsQuery</c>.
/// </summary>
public sealed record GetPettyCashFundSettlementsQuery(Guid FundId) : IRequest<IReadOnlyList<PettyCashSettlementHistoryItemDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
