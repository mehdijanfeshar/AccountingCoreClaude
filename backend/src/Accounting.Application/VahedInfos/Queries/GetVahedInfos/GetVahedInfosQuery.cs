using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.VahedInfos.Queries.GetVahedInfos;

/// <summary>
/// Returns a page of <c>TB_VAHED_INFO</c> rows projected to <see cref="VahedInfoDto"/>. No
/// logical-delete filter is applied — the table has no <c>ISDELETED</c> column, so every row
/// is included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetVahedInfosQueryValidator.MaxPageSize"/>.</param>
public sealed record GetVahedInfosQuery(int PageNumber, int PageSize) : IRequest<PagedResult<VahedInfoDto>>;
