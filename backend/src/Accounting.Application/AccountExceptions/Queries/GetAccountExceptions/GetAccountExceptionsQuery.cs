using Accounting.Application.Common;
using MediatR;

namespace Accounting.Application.AccountExceptions.Queries.GetAccountExceptions;

/// <summary>
/// Returns a page of <c>TB_ACCOUNTEXCEPTION</c> rows projected to <see cref="AccountExceptionDto"/>.
/// Only non-deleted rows are included.
/// </summary>
/// <param name="PageNumber">1-based page number.</param>
/// <param name="PageSize">Page size, capped by <see cref="GetAccountExceptionsQueryValidator.MaxPageSize"/>.</param>
public sealed record GetAccountExceptionsQuery(int PageNumber, int PageSize)
    : IRequest<PagedResult<AccountExceptionDto>>;
