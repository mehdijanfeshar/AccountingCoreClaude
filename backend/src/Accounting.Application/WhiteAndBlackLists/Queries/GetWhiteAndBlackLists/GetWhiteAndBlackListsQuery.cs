using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.WhiteAndBlackLists.Queries.GetWhiteAndBlackLists;

/// <summary>
/// Returns a page of <c>TB_WHITEANDBLACKLIST</c> rows projected to
/// <see cref="WhiteAndBlackListDto"/>. Only non-deleted rows (<c>ISDELETED != true</c>) are
/// included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetWhiteAndBlackListsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetWhiteAndBlackListsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<WhiteAndBlackListDto>>;
