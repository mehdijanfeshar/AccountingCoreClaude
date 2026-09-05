using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.RevolvingFunds.Queries.GetRevolvingFunds;

/// <summary>
/// Returns a page of <c>TB_REVOLVING_FUND</c> rows projected to <see cref="RevolvingFundDto"/>.
/// Only non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetRevolvingFundsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetRevolvingFundsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<RevolvingFundDto>>;
