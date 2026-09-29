using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetPaymentRequestById;

/// <summary><c>GET api/treasury/payment-requests/{id}</c> — with events.</summary>
public sealed record GetPaymentRequestByIdQuery(Guid Id) : IRequest<PaymentRequestDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
