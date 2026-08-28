using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.WorkShops.Queries.GetWorkShops;

/// <summary>
/// Returns a page of <c>TB_WORKSHOP</c> rows projected to <see cref="WorkShopDto"/>. Only
/// non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetWorkShopsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetWorkShopsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<WorkShopDto>>;
