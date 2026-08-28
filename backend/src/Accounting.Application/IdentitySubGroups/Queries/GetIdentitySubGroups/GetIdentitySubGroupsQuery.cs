using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.IdentitySubGroups.Queries.GetIdentitySubGroups;

/// <summary>
/// Returns a page of <c>TB_IDENTITYSUBGRP</c> rows projected to <see cref="IdentitySubGroupDto"/>.
/// Only non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetIdentitySubGroupsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetIdentitySubGroupsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<IdentitySubGroupDto>>;
