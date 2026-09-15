using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.TafsilGroups.Queries.GetTafsilGroups;

/// <summary>
/// Returns a page of <c>TB_TAFSIL_GROUP</c> rows projected to <see cref="TafsilGroupDto"/>. Only
/// non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetTafsilGroupsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetTafsilGroupsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<TafsilGroupDto>>;
