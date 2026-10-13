using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.Queries.Treasury;
using MediatR;

namespace Accounting.Application.Queries.Treasury.GetTreasuryDashboard;

/// <summary><c>GET api/treasury/dashboard</c>.</summary>
public sealed record GetTreasuryDashboardQuery : IRequest<TreasuryDashboardDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
