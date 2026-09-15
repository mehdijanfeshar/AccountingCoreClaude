using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.Rabets.Queries.GetRabets;

/// <summary>
/// Returns a page of <c>TB_RABET</c> rows projected to <see cref="RabetDto"/>. Only non-deleted
/// rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetRabetsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetRabetsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<RabetDto>>;
