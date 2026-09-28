using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashReplenishmentPreview;

/// <summary>
/// <c>GET api/petty-cash/funds/{fundId}/replenishment-preview</c> — بخش ۳-الف
/// (<c>docs/tankhah-khazaneh-module.md</c>). Returns <see langword="null"/> when the fund does not
/// exist (mapped to 404 by the controller).
/// </summary>
public sealed record GetPettyCashReplenishmentPreviewQuery(Guid FundId) : IRequest<PettyCashReplenishmentPreviewDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
