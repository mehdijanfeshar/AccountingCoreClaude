using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.Accounts.Queries.GetAccountCodes;

/// <summary>
/// Returns a page of <c>TB_ACCOUNTCODE</c> rows projected to <see cref="AccountCodeDto"/>.
/// Only non-deleted rows (<c>ISDELETED != true</c>) are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetAccountCodesQueryValidator.MaxPageSize"/>.</param>
/// <param name="Search">
/// Optional search over the WHOLE chart, not one page: codes that start with it, or titles that
/// contain it. Persian/Arabic digits are accepted. Empty means no filter.
/// </param>
public sealed record GetAccountCodesQuery(int PageNumber, int PageSize, string? Search = null) : IRequest<PagedResult<AccountCodeDto>>;
