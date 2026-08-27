using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.AccountCodeInterfaces.Queries.GetAccountCodeInterfaces;

/// <summary>
/// Returns a page of <c>TB_ACCOUNTCODE_INTERFACE</c> rows projected to
/// <see cref="AccountCodeInterfaceDto"/>. Only non-deleted rows are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetAccountCodeInterfacesQueryValidator.MaxPageSize"/>.</param>
public sealed record GetAccountCodeInterfacesQuery(int PageNumber, int PageSize)
    : IRequest<PagedResult<AccountCodeInterfaceDto>>;
