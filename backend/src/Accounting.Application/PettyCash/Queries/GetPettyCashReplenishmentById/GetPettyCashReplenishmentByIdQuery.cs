using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashReplenishmentById;

/// <summary>
/// <c>GET api/petty-cash/replenishments/{id}</c> — returns <see langword="null"/> when no row
/// with the given <see cref="Id"/> exists (mapped to 404 by the controller).
/// </summary>
public sealed record GetPettyCashReplenishmentByIdQuery(Guid Id) : IRequest<PettyCashReplenishmentDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
