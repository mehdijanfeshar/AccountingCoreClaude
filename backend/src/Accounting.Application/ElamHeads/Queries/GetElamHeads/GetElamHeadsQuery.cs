using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.ElamHeads.Queries.GetElamHeads;

/// <summary>
/// Returns a page of <c>TB_ELAMHEAD</c> rows projected to <see cref="ElamHeadDto"/>. Only
/// non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetElamHeadsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetElamHeadsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<ElamHeadDto>>;
