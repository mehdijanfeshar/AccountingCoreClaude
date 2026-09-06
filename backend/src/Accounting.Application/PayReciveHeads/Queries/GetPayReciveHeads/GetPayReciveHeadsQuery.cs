using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.PayReciveHeads.Queries.GetPayReciveHeads;

/// <summary>
/// Returns a page of <c>TB_PAYRECIVHEAD</c> rows projected to <see cref="PayReciveHeadDto"/>.
/// Only non-deleted rows (<c>ISDELETED == false</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetPayReciveHeadsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetPayReciveHeadsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<PayReciveHeadDto>>;
