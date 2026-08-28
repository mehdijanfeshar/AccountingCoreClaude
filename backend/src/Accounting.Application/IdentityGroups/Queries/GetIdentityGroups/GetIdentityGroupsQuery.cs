using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.IdentityGroups.Queries.GetIdentityGroups;

/// <summary>
/// Returns a page of <c>TB_IDENTITYGROUP</c> rows projected to <see cref="IdentityGroupDto"/>.
/// Only non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetIdentityGroupsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetIdentityGroupsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<IdentityGroupDto>>;
