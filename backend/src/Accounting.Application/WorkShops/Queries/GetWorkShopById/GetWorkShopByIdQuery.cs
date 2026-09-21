using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.WorkShops.Queries.GetWorkShopById;

/// <summary>
/// Returns a single <c>TB_WORKSHOP</c> row projected to <see cref="WorkShopDto"/>, or
/// <see langword="null"/> if no row with the given <see cref="Id"/> exists. Unlike the list
/// query, no logical-delete filter is applied — the row is returned regardless of
/// <c>ISDELETED</c>, and <see cref="WorkShopDto.IsDeleted"/> lets the caller decide.
/// </summary>
/// <para>
/// <see cref="IVahedScopedQuery"/> as of the record-ownership work: until then a by-id lookup
/// carried no unit at all, so any caller who knew an <c>ID</c> could read any unit's row — the
/// second half of IDOR risk #1. The list query has been scoped since phase 19; this one had not.
/// </para>
/// <param name="Id">ID column to look up.</param>
public sealed record GetWorkShopByIdQuery(Guid Id) : IRequest<WorkShopDto?>, IVahedScopedQuery
{
    /// <summary>
    /// Caller's own organizational unit. Server-assigned by <c>VahedScopeBehavior</c> from the
    /// authenticated principal — never bound from client input, hence
    /// <see cref="JsonIgnoreAttribute"/> (the controller builds this query from the route id, but
    /// the attribute keeps it out of the Swagger schema and matches every other
    /// <see cref="IVahedScopedQuery"/>).
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
