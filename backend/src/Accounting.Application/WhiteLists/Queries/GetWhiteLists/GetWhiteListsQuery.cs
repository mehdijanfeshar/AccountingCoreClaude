using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.WhiteLists.Queries.GetWhiteLists;

/// <summary>
/// Returns a page of <c>TB_WHITELIST</c> rows projected to <see cref="WhiteListDto"/>. Only
/// non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetWhiteListsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetWhiteListsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<WhiteListDto>>;
