using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.PettyCash.Queries.GetPettyCashFundById;

/// <summary>
/// <c>GET api/petty-cash/funds/{fundId}</c> — returns a single <c>TB_PC_FUND</c> row projected to
/// <see cref="PettyCashFundDto"/>, or <see langword="null"/> if no row with the given
/// <see cref="Id"/> exists (mapped to 404 by the controller).
/// </summary>
public sealed record GetPettyCashFundByIdQuery(Guid Id) : IRequest<PettyCashFundDto?>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
