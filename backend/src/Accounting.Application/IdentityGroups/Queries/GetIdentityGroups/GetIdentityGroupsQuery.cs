using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.IdentityGroups.Queries.GetIdentityGroups;

/// <summary>
/// Returns a page of <c>TB_IDENTITYGROUP</c> rows projected to <see cref="IdentityGroupDto"/>.
/// Only non-deleted rows (<c>ISDELETED != true</c>) are included, and — via
/// <see cref="IVahedScopedQuery"/> — only rows belonging to the caller's own organizational unit
/// (<c>VAHEDCODE == VahedCode</c>, never <c>OR VAHEDCODE IS NULL</c>; see
/// <c>IdentityGroupReadRepository.GetPagedAsync</c> for why the filter is unconditional).
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetIdentityGroupsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetIdentityGroupsQuery(int PageNumber, int PageSize)
    : IRequest<PagedResult<IdentityGroupDto>>, IVahedScopedQuery
{
    /// <summary>
    /// Organizational unit code to filter by. Server-assigned by <c>VahedScopeBehavior</c> from
    /// the authenticated caller's own <c>VahedCode</c> — never bound from client input (the
    /// controller builds this query from individually-bound query-string parameters, not a
    /// deserialized body, but <see cref="JsonIgnoreAttribute"/> is still applied here for
    /// consistency with every other <see cref="IVahedScopedQuery"/>/<c>IVahedScopedCommand</c>
    /// implementer and to keep it out of the Swagger schema).
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
