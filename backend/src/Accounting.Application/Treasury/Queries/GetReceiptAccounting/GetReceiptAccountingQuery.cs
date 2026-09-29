using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetReceiptAccounting;

/// <summary><c>GET api/treasury/receipts/{id}/accounting</c>.</summary>
public sealed record GetReceiptAccountingQuery(Guid Id) : IRequest<ReceiptAccountingDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
