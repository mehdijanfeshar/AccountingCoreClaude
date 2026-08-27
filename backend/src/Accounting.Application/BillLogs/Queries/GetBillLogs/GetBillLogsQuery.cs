using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.BillLogs.Queries.GetBillLogs;

/// <summary>
/// Returns a page of <c>TB_BILL_LOG</c> rows projected to <see cref="BillLogDto"/>. Only
/// non-deleted rows are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetBillLogsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetBillLogsQuery(int PageNumber, int PageSize) : IRequest<PagedResult<BillLogDto>>;
