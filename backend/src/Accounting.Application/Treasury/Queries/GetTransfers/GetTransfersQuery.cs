using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetTransfers;

/// <summary><c>GET api/treasury/transfers?state=&amp;search=&amp;pageNumber=&amp;pageSize=</c>.</summary>
public sealed record GetTransfersQuery(
    int PageNumber,
    int PageSize,
    TransferState? State,
    string? Search) : IRequest<TransferListResult>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
