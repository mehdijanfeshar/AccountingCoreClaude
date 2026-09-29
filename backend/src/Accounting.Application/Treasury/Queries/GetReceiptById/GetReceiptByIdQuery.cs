using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetReceiptById;

/// <summary><c>GET api/treasury/receipts/{id}</c>.</summary>
public sealed record GetReceiptByIdQuery(Guid Id) : IRequest<ReceiptDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
