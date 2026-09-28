using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundDashboard;

/// <summary>
/// <c>GET api/petty-cash/funds/{fundId}/dashboard</c> — بخش ۳-الف (<c>docs/tankhah-khazaneh-module.md</c>،
/// صفحهٔ ۴ پاورپوینت). Returns <see langword="null"/> when the fund does not exist (mapped to 404
/// by the controller).
/// </summary>
public sealed record GetPettyCashFundDashboardQuery(Guid FundId) : IRequest<PettyCashFundDashboardDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
