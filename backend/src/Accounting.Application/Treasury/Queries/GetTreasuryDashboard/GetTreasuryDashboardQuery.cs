using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Application.Treasury.Queries;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetTreasuryDashboard;

/// <summary><c>GET api/treasury/dashboard</c>.</summary>
public sealed record GetTreasuryDashboardQuery : IRequest<TreasuryDashboardDto>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
