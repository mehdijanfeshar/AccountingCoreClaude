using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Queries.Treasury.GetReceipts;

/// <summary><c>GET api/treasury/receipts?state=&amp;search=&amp;pageNumber=&amp;pageSize=</c>.</summary>
public sealed record GetReceiptsQuery(
    int PageNumber,
    int PageSize,
    ReceiptState? State,
    string? Search) : IRequest<ReceiptListResult>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
