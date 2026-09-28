using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.PettyCash.Queries;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundSettlementPreview;

/// <summary>
/// <c>GET api/petty-cash/funds/{fundId}/settlement</c> — بخش ۳-ب
/// (<c>docs/tankhah-khazaneh-module.md</c> section 9). Returns <see langword="null"/> when the
/// fund does not exist (mapped to 404 by the controller).
/// </summary>
public sealed record GetPettyCashFundSettlementPreviewQuery(Guid FundId) : IRequest<PettyCashSettlementPreviewDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
