using System.Text.Json.Serialization;
using Accounting.Application.Common.Security;
using MediatR;

namespace Accounting.Application.Queries.Treasury.GetTreasuryRoles;

/// <summary><c>GET api/treasury/roles</c> — bare array, small unpaged list.</summary>
public sealed record GetTreasuryRolesQuery : IRequest<IReadOnlyList<TreasuryRoleDto>>, IVahedScopedQuery
{
    [JsonIgnore]
    public string VahedCode { get; set; } = string.Empty;
}
