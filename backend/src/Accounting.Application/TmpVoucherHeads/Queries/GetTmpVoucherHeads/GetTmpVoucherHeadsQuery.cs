using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.TmpVoucherHeads.Queries.GetTmpVoucherHeads;

/// <summary>
/// Returns a page of <c>TB_TMP_VOUCHERHEAD</c> rows projected to
/// <see cref="TmpVoucherHeadDto"/>. Only non-deleted rows (<c>ISDELETED != true</c>) are
/// included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetTmpVoucherHeadsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetTmpVoucherHeadsQuery(int PageNumber, int PageSize)
    : IRequest<PagedResult<TmpVoucherHeadDto>>;
