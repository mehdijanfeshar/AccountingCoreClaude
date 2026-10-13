using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Queries.Treasury.GetTransferAccounting;

/// <summary><c>GET api/treasury/transfers/{id}/accounting</c>.</summary>
public sealed record GetTransferAccountingQuery(Guid Id) : IRequest<TransferAccountingDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
