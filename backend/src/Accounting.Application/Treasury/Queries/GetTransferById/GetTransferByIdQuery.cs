using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Treasury.Queries.GetTransferById;

/// <summary><c>GET api/treasury/transfers/{id}</c> — with events.</summary>
public sealed record GetTransferByIdQuery(Guid Id) : IRequest<TransferDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
