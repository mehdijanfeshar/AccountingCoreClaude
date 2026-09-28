using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashRefunds;

/// <summary><c>GET api/petty-cash/funds/{fundId}/refunds</c> — بخش ۳-الف.</summary>
public sealed record GetPettyCashRefundsQuery(Guid FundId) : IRequest<IReadOnlyList<PettyCashRefundDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
