using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetPaymentRequestAccounting;

/// <summary><c>GET api/treasury/payment-requests/{id}/accounting</c> — بخش ۴-ب.</summary>
public sealed record GetPaymentRequestAccountingQuery(Guid Id) : IRequest<PaymentRequestAccountingDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
