using System.Text.Json.Serialization;
using Accounting.Application.Common;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.IdentityHeads.Queries.GetIdentityHeads;

/// <summary>
/// Returns a page of شناسنامه records (<c>TB_IDENTITYHEAD</c>) with their fixed values, projected
/// to <see cref="IdentityHeadDto"/>. Only non-deleted rows are included, and — via
/// <see cref="IVahedScopedQuery"/> — only rows of the caller's own organizational unit.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size.</param>
/// <param name="IdentityGroupId">Optional: only the شناسنامه records of this group.</param>
/// <param name="Year">Optional: only records of this fiscal year.</param>
public sealed record GetIdentityHeadsQuery(
    int PageNumber,
    int PageSize,
    Guid? IdentityGroupId = null,
    string? Year = null)
    : IRequest<PagedResult<IdentityHeadDto>>, IVahedScopedQuery
{
    /// <summary>
    /// Organizational unit code to filter by. Server-assigned by <c>VahedScopeBehavior</c> from
    /// the authenticated caller — never bound from client input.
    /// </summary>
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
