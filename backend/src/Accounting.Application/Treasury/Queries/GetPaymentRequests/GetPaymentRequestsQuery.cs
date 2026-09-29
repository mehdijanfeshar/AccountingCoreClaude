using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using Accounting.Domain.ValueObjects;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetPaymentRequests;

/// <summary><c>GET api/treasury/payment-requests?state=&amp;search=&amp;pageNumber=&amp;pageSize=</c>.</summary>
public sealed record GetPaymentRequestsQuery(
    int PageNumber,
    int PageSize,
    PaymentRequestState? State,
    string? Search) : IRequest<PaymentRequestListResult>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
