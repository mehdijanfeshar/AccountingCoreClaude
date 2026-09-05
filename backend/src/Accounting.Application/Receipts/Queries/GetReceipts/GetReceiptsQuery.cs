using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.Receipts.Queries.GetReceipts;

/// <summary>
/// Returns a page of <c>TB_RECEIP</c> rows projected to <see cref="ReceiptDto"/>. Only
/// non-deleted rows (<c>ISDELETED == false</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetReceiptsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetReceiptsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<ReceiptDto>>;
