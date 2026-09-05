using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.CheckBooks.Queries.GetCheckBooks;

/// <summary>
/// Returns a page of <c>TB_CHECKBOOK</c> rows projected to <see cref="CheckBookDto"/>. Only
/// non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetCheckBooksQueryValidator.MaxPageSize"/>.</param>
public sealed record GetCheckBooksQuery(int PageNumber, int PageSize) : IRequest<PagedResult<CheckBookDto>>;
