using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.ChequeTypes.Queries.GetChequeTypes;

/// <summary>
/// Returns a page of <c>TB_CHECK_TYPE</c> rows projected to <see cref="ChequeTypeDto"/>. Only
/// non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetChequeTypesQueryValidator.MaxPageSize"/>.</param>
public sealed record GetChequeTypesQuery(int PageNumber, int PageSize) : IRequest<PagedResult<ChequeTypeDto>>;
